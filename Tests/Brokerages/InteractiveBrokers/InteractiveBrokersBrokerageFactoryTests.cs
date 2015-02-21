﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersBrokerageFactoryTests
    {
        [Test]
        public void InitializesInstanceFromComposer()
        {
            var composer = Composer.Instance;
            var factory = composer.Single<IBrokerageFactory>(instance => instance.BrokerageType == typeof(InteractiveBrokersBrokerage));
            Assert.IsNotNull(factory);

            var job = new LiveNodePacket { Brokerage = "Interactive Brokers"};
            var brokerage = factory.CreateBrokerage(job, null); // IB factory doesn't use IAlgorithm instance
            Assert.IsNotNull(brokerage);
            Assert.IsInstanceOf<InteractiveBrokersBrokerage>(brokerage);
        }
    }
}
