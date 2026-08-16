#pragma once

#include <vector>
#include <functional>
#include <mutex>
#include <memory>
#include <atomic>
#include <algorithm>
#include <cstdint>
#include <variant>
#include "ThreadSafe.h"
#include "NonThreadSafe.h"

namespace engine {
    namespace event_broker {
        template <typename MessageType>
        class NonThreadSafeBus {
        public:
            using Stub = void(*)(void*, const MessageType&);

            struct Subscriber {
                void* object;
                Stub stub;
            };

            NonThreadSafeBus() {
                subscribers_.reserve(8);
            }

            template<typename TObject, auto Method>
            static void stub_helper(void* object, const MessageType& message) {
                (static_cast<TObject*>(object)->*Method)(message);
            }

            template<typename TObject, auto Method>
            void subscribe(TObject* object) {
                subscribers_.push_back({object, &stub_helper<TObject, Method>});
            }

            // 특정 이벤트 타입만 받아 핸들러에 풀어 전달 (MessageType 이 std::variant 일 때)
            template<typename TObject, typename TEvent, auto Method>
            static void typed_stub_helper(void* object, const MessageType& message) {
                if (const auto* event = std::get_if<TEvent>(&message)) {
                    (static_cast<TObject*>(object)->*Method)(*event);
                }
            }

            template<typename TObject, typename TEvent, auto Method>
            void subscribe(TObject* object) {
                subscribers_.push_back({object, &typed_stub_helper<TObject, TEvent, Method>});
            }

            void publish(const MessageType& message) {
                const Subscriber* const data = subscribers_.data();
                const size_t size = subscribers_.size();
                for (size_t i = 0; i < size; ++i) {
                    data[i].stub(data[i].object, message);
                }
            }

            void clear() {
                subscribers_.clear();
            }

        private:
            std::vector<Subscriber> subscribers_;
        };

    }
} 