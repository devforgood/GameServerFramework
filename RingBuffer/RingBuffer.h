#pragma once
#include <array>
#include <bit>
#include <type_traits>
#include <vector>
#include <optional>
#include <span>
#include <algorithm>
#include <concepts>
#include <cstddef>
#include <ranges>
#include <memory>

template<typename T>
concept TriviallyCopyable = std::is_trivially_copyable_v<T>;

template<typename T>
concept Numeric = std::integral<T> || std::floating_point<T>;

class RingBuffer {
public:
    using size_type = std::size_t;
    using value_type = char;
    using reference = value_type&;
    using const_reference = const value_type&;
    using pointer = value_type*;
    using const_pointer = const value_type*;

    // 생성자 - constexpr로 컴파일 타임 최적화 가능
    constexpr explicit RingBuffer(size_type capacity)
        : buffer_(capacity), head_(0), tail_(0), size_(0) {
    }

    // 복사 생성자
    constexpr RingBuffer(const RingBuffer& other) = default;
    
    // 이동 생성자
    constexpr RingBuffer(RingBuffer&& other) noexcept = default;
    
    // 복사 할당 연산자
    constexpr RingBuffer& operator=(const RingBuffer& other) = default;
    
    // 이동 할당 연산자
    constexpr RingBuffer& operator=(RingBuffer&& other) noexcept = default;

    // 쓰기 포인터와 사용 가능한 크기 반환
    [[nodiscard]] constexpr pointer write_ptr(size_type& out_size) noexcept {
        if (size_ == buffer_.size()) {
            out_size = 0;
            return nullptr;
        }

        if (tail_ >= head_) {
            // tail이 head보다 뒤에 있는 경우
            const size_type available_to_end = buffer_.size() - tail_;
            const size_type available_before_head = head_;
            out_size = std::min(available_to_end, buffer_.size() - size_);
        } else {
            // tail이 head보다 앞에 있는 경우
            out_size = head_ - tail_;
        }

        return std::addressof(buffer_[tail_]);
    }

    // 쓰기 커밋
    constexpr void commit_write(size_type len) noexcept {
        if (len == 0) return;
        
        tail_ = (tail_ + len) % buffer_.size();
        size_ += len;
    }

    // 읽기 포인터 확인 (연속된 데이터만)
    [[nodiscard]] constexpr bool peek_ptr(const_pointer& ptr, size_type length) const noexcept {
        if (size_ < length) return false;
        
        if (head_ + length <= buffer_.size()) {
            ptr = std::addressof(buffer_[head_]);
            return true;
        }
        return false;
    }

    // 읽기 포인터와 크기 반환 (연속된 데이터만)
    struct PeekResult {
        const_pointer ptr;
        size_type length;
        
        constexpr operator std::span<const value_type>() const noexcept {
            return std::span<const value_type>(ptr, length);
        }
    };
    
    [[nodiscard]] constexpr std::optional<PeekResult> try_peek_span(size_type max_length) const noexcept {
        if (size_ == 0) return std::nullopt;
        
        const size_type available_length = std::min(size_, max_length);
        if (head_ + available_length <= buffer_.size()) {
            return PeekResult{std::addressof(buffer_[head_]), available_length};
        }
        return std::nullopt;
    }

    // std::span으로 데이터 접근
    [[nodiscard]] constexpr std::optional<std::span<const value_type>> try_peek_span_cpp20(size_type max_length) const noexcept {
        if (size_ == 0) return std::nullopt;
        
        const size_type available_length = std::min(size_, max_length);
        if (head_ + available_length <= buffer_.size()) {
            return std::span<const value_type>(std::addressof(buffer_[head_]), available_length);
        }
        return std::nullopt;
    }

    // 데이터 복사 없이 읽기
    [[nodiscard]] constexpr bool peek(pointer dest, size_type len) const noexcept {
        if (size_ < len) return false;
        
        size_type idx = head_;
        for (size_type i = 0; i < len; ++i) {
            dest[i] = buffer_[idx];
            idx = (idx + 1) % buffer_.size();
        }
        return true;
    }

    // 헤더처럼 고정 크기 값 하나를 그대로 꺼낸다.
    // 링 경계에 걸쳐 있는지 여부는 버퍼가 알아서 처리하므로, 호출부가
    // 연속/분할 두 경로를 따로 들고 있을 필요가 없다.
    template<TriviallyCopyable T>
    [[nodiscard]] constexpr std::optional<T> peek_value() const noexcept {
        std::array<value_type, sizeof(T)> raw{};
        if (!peek(raw.data(), raw.size())) return std::nullopt;
        return std::bit_cast<T>(raw);
    }

    // 데이터 읽기 (복사 여부 선택 가능)
    constexpr void read(pointer dest, size_type len) noexcept {
        if (len > size_) len = size_; // 안전성 체크
        
        if (dest) {
            // 데이터 복사하면서 읽기
            for (size_type i = 0; i < len; ++i) {
                dest[i] = buffer_[head_];
                head_ = (head_ + 1) % buffer_.size();
            }
        } else {
            // 데이터 복사 없이 읽기만
            head_ = (head_ + len) % buffer_.size();
        }
        size_ -= len;
    }

    // 남은 데이터를 버퍼 앞쪽으로 몰아 head_ 를 0 으로 만든다.
    // 쪼개져 있던 데이터가 연속이 되고, 연속 쓰기 창도 available_space() 만큼 넓어진다.
    //
    // 주의: 버퍼 내부를 가리키던 포인터/스팬(write_ptr, peek_ptr, try_peek_span 의 결과)은
    // 전부 무효가 된다. 진행 중인 비동기 읽기가 그 포인터를 들고 있다면 호출하면 안 된다.
    constexpr void normalize() noexcept {
        if (size_ == 0) {
            head_ = tail_ = 0;
            return;
        }
        if (head_ == 0) return;

        if (head_ + size_ <= buffer_.size()) {
            // 연속 구간 - 앞으로 당기기만 하면 된다(dest 가 src 보다 앞이라 겹쳐도 안전).
            std::copy(buffer_.begin() + head_, buffer_.begin() + head_ + size_, buffer_.begin());
        } else {
            // 끝과 처음으로 쪼개져 있다 - 회전시켜 이어 붙인다.
            std::rotate(buffer_.begin(), buffer_.begin() + head_, buffer_.end());
        }

        head_ = 0;
        tail_ = size_ % buffer_.size();
    }

    // 데이터 끝 뒤쪽에 남아 있는 연속 공간.
    // contiguous_write_size 와 달리 head_ '앞쪽' 의 빈 공간은 세지 않는다. 그쪽에 쓰면
    // 데이터가 경계를 넘어 쪼개지기 때문이다. 이미 쪼개져 있으면 0 이다.
    [[nodiscard]] constexpr size_type trailing_write_size() const noexcept {
        if (head_ + size_ >= buffer_.size()) return 0;
        return buffer_.size() - (head_ + size_);
    }

    // 다음 쓰기가 링 경계를 넘지 않도록, 데이터 뒤에 desired 만큼의 연속 공간을 확보한다.
    // 성공하면 write_ptr 은 반드시 데이터 끝에 이어지는 영역을 돌려주므로, 매 쓰기 전에
    // 이 함수를 부르는 한 버퍼 안의 데이터는 절대 쪼개지지 않는다(= peek 계열이 항상 성공).
    // 실패는 남은 데이터가 그만큼 많아 자리를 못 만든다는 뜻이다.
    [[nodiscard]] constexpr bool reserve_linear_write(size_type desired) noexcept {
        if (trailing_write_size() >= desired) return true;

        normalize();
        return trailing_write_size() >= desired;
    }

    // 범위 기반 읽기 (C++20 ranges 활용)
    [[nodiscard]] constexpr auto read_range(size_type max_length) const noexcept {
        return std::views::iota(size_type{0}, std::min(size_, max_length))
               | std::views::transform([this](size_type i) {
                   return buffer_[(head_ + i) % buffer_.size()];
               });
    }

    // 버퍼 비우기
    constexpr void clear() noexcept {
        head_ = tail_ = size_ = 0;
    }

    // 버퍼가 비어있는지 확인
    [[nodiscard]] constexpr bool empty() const noexcept { 
        return size_ == 0; 
    }

    // 버퍼가 가득 찬지 확인
    [[nodiscard]] constexpr bool full() const noexcept { 
        return size_ == buffer_.size(); 
    }

    // 현재 데이터 크기
    [[nodiscard]] constexpr size_type size() const noexcept { 
        return size_; 
    }

    // 버퍼 용량
    [[nodiscard]] constexpr size_type capacity() const noexcept { 
        return buffer_.size(); 
    }

    // 사용 가능한 공간
    [[nodiscard]] constexpr size_type available_space() const noexcept { 
        return buffer_.size() - size_; 
    }

    // 버퍼 사용률 (0.0 ~ 1.0)
    [[nodiscard]] constexpr double usage_ratio() const noexcept {
        return static_cast<double>(size_) / static_cast<double>(buffer_.size());
    }

    // 연속된 읽기 가능한 데이터 크기
    [[nodiscard]] constexpr size_type contiguous_read_size() const noexcept {
        if (size_ == 0) return 0;
        
        if (head_ + size_ <= buffer_.size()) {
            return size_;
        }
        return buffer_.size() - head_;
    }

    // 연속된 쓰기 가능한 공간 크기
    [[nodiscard]] constexpr size_type contiguous_write_size() const noexcept {
        if (size_ == buffer_.size()) return 0;
        
        if (tail_ >= head_) {
            return buffer_.size() - tail_;
        }
        return head_ - tail_;
    }

    // 버퍼 상태 정보
    struct BufferInfo {
        size_type size;
        size_type capacity;
        size_type head;
        size_type tail;
        double usage_ratio;
        size_type contiguous_read;
        size_type contiguous_write;
    };

    [[nodiscard]] constexpr BufferInfo get_info() const noexcept {
        return BufferInfo{
            .size = size_,
            .capacity = buffer_.size(),
            .head = head_,
            .tail = tail_,
            .usage_ratio = usage_ratio(),
            .contiguous_read = contiguous_read_size(),
            .contiguous_write = contiguous_write_size()
        };
    }

private:
    std::vector<value_type> buffer_;
    size_type head_, tail_, size_;
};