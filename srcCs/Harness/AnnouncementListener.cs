/*
 * (c) 2005-2021 Copyright, Real-Time Innovations, Inc. All rights reserved.
 * Subject to Eclipse Public License v1.0; see LICENSE.md for details.
 */

using System.Collections.Generic;

namespace PerformanceTest
{
    public class AnnouncementListener : IMessagingCallback
    {
        private readonly object subscriberLock = new object();
        private readonly HashSet<int> activeSubscribers = new HashSet<int>();

        public int ActiveSubscriberCount
        {
            get
            {
                lock (subscriberLock)
                {
                    return activeSubscribers.Count;
                }
            }
        }

        public void ProcessMessage(TestMessage message)
        {
            lock (subscriberLock)
            {
                if (message.Size == Perftest.INITIALIZE_SIZE)
                {
                    activeSubscribers.Add(message.entityId);
                }
                else if (message.Size == Perftest.FINISHED_SIZE)
                {
                    activeSubscribers.Remove(message.entityId);
                }
            }
        }
    }
} // PerformanceTest Namespace
