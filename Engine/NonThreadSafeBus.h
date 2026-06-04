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

namespace Engine {
    namespace EventBroker {

        template <typename MessageType>
        class NonThreadSafeBus {
        public:
            using Callback = std::function<void(const MessageType&)>;
            using Token = uint64_t;

            NonThreadSafeBus() {
            }

            Token subscribe(Callback callback) {
                Token token = nextToken_ ++;

                subscribers_.push_back({ token, std::move(callback) });

                return token;
            }

            void unsubscribe(Token token) {
                auto it = std::remove_if(subscribers_.begin(), subscribers_.end(),
                    [token](const Subscriber& sub) { return sub.token == token; });

                if (it != subscribers_.end()) {
                    subscribers_.erase(it, subscribers_.end());
                }
            }

            void publish(const MessageType& message) {

                for (const auto& sub : subscribers_) {
                    sub.callback(message);
                }
            }

            void clear() {
                subscribers_.clear();
            }

        private:
            struct Subscriber {
                Token token;
                Callback callback;
            };

            std::vector<Subscriber> subscribers_;
            Token nextToken_ = 1;
        };

    } // namespace EventBroker
} // namespace Engine