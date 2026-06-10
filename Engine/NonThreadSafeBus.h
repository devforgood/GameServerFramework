#pragma once

#include <vector>
#include <functional>
#include <mutex>
#include <memory>
#include <atomic>
#include <algorithm>
#include <cstdint>
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