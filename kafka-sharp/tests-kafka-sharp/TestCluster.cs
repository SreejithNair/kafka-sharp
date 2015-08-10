﻿using System.Threading;
using System.Threading.Tasks;
using Kafka.Cluster;
using Kafka.Protocol;
using Kafka.Public;
using Kafka.Routing;
using NUnit.Framework;
using Cluster = Kafka.Cluster.Cluster;

namespace tests_kafka_sharp
{
    [TestFixture]
    class TestCluster
    {
        

        void AssertRouting(RoutingTable routing)
        {
            var p1 = routing.GetPartitions("topic1");
            var p2 = routing.GetPartitions("topic2");
            var p3 = routing.GetPartitions("topic3");
            var e1 = routing.GetPartitions("error1");
            var e2 = routing.GetPartitions("error2");

            Assert.NotNull(p1);
            Assert.NotNull(p2);
            Assert.NotNull(p3);
            Assert.NotNull(e1);
            Assert.NotNull(e2);

            Assert.AreEqual(0, e1.Length);

            Assert.AreEqual(1, e2.Length);
            Assert.IsNotNull(e2[0].Leader);
            Assert.AreEqual(2, e2[0].Id);

            Assert.AreEqual(1, p1.Length);
            Assert.IsNotNull(p1[0].Leader);
            Assert.AreEqual(1, p1[0].Id);

            Assert.AreEqual(2, p2.Length);
            Assert.IsNotNull(p2[0].Leader);
            Assert.AreEqual(1, p2[0].Id);
            Assert.IsNotNull(p2[1].Leader);
            Assert.AreEqual(2, p2[1].Id);

            Assert.AreEqual(3, p3.Length);
            Assert.IsNotNull(p3[0].Leader);
            Assert.AreEqual(1, p3[0].Id);
            Assert.IsNotNull(p3[1].Leader);
            Assert.AreEqual(2, p3[1].Id);
            Assert.IsNotNull(p3[2].Leader);
            Assert.AreEqual(3, p3[2].Id);

            Assert.AreSame(p1[0].Leader, p2[0].Leader);
            Assert.AreSame(p2[1].Leader, p3[1].Leader);
            Assert.AreNotSame(p2[1].Leader, p3[2].Leader);
            Assert.AreNotSame(p1[0].Leader, p3[2].Leader);
        }

        [Test]
        public async Task TestFetchRoutingTable()
        {
            int errors = 0;
            var cluster = new Cluster(new Configuration {Seeds = "localhost:1"}, new DevNullLogger(),
                                      (h, p) => new NodeMock(TestData.TestMetadataResponse),
                                      () => new ProduceRouterMock());
            cluster.InternalError += _ => ++errors;
            cluster.Start();
            var routing = await cluster.RequireNewRoutingTable();

            Assert.AreEqual(0, errors);
            AssertRouting(routing);
        }

        [Test]
        public void TestSignalRoutingTable()
        {
            int errors = 0;
            var ev = new ManualResetEvent(false);
            RoutingTable route = null;
            var router = new ProduceRouterMock();
            router.OnChangeRouting += r =>
                {
                    route = r;
                    ev.Set();
                };
            var cluster = new Cluster(new Configuration {Seeds = "localhost:1"}, new DevNullLogger(),
                                      (h, p) => new NodeMock(TestData.TestMetadataResponse),
                                      () => router);
            cluster.InternalError += _ => ++errors;
            cluster.Start();
            ev.WaitOne();
            Assert.AreEqual(0, errors);
            AssertRouting(route);
        }
    }
}
