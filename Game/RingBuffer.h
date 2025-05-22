#pragma once
#include <vector>
#include <optional>
#include <span>
#include <algorithm>
#include <cstddef> // for size_t
    
class RingBuffer {
public:
    RingBuffer(size_t capacity)
        : buffer_(capacity), head_(0), tail_(0), size_(0) {
    }

    char* write_ptr(size_t& out_size) {
        if (size_ == buffer_.size()) {
            out_size = 0;
            return nullptr;
        }

        if (tail_ >= head_)
            out_size = std::min(buffer_.size() - tail_, buffer_.size() - size_);
        else
            out_size = head_ - tail_ - 1;

        return &buffer_[tail_];
    }

    void commit_write(size_t len) {
        tail_ = (tail_ + len) % buffer_.size();
        size_ += len;
    }

    bool peek_ptr(const char*& ptr, size_t length) const {
        if (size_ < length) return false;
        if (head_ + length <= buffer_.size()) {
            ptr = &buffer_[head_];
            return true;
        }
        return false;
    }

    std::optional<std::span<const char>> try_peek_span(size_t length) const {
        if (size_ < length) return std::nullopt;
        if (head_ + length <= buffer_.size())
            return std::span<const char>(&buffer_[head_], length);
        return std::nullopt;
    }

    bool peek(char* dest, size_t len) const {
        if (size_ < len) return false;
        size_t idx = head_;
        for (size_t i = 0; i < len; ++i) {
            dest[i] = buffer_[idx];
            idx = (idx + 1) % buffer_.size();
        }
        return true;
    }

    void read(char* dest, size_t len) {
        if (dest) {
            for (size_t i = 0; i < len; ++i) {
                dest[i] = buffer_[head_];
                head_ = (head_ + 1) % buffer_.size();
                --size_;
            }
        }
        else {
            for (size_t i = 0; i < len; ++i) {
                head_ = (head_ + 1) % buffer_.size();
                --size_;
            }
        }
    }

    size_t size() const { return size_; }

private:
    std::vector<char> buffer_;
    size_t head_, tail_, size_;
};