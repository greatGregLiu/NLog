// 
// Copyright (c) 2004-2010 Jaroslaw Kowalski <jaak@jkowalski.net>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

namespace NLog.UnitTests.Targets
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NLog.Internal;
    using NLog.Targets;

    [TestClass]
    public class TargetTests : NLogTestBase
    {
        [TestMethod]
        public void InitializeTest()
        {
            var target = new MyTarget();
            ((ISupportsInitialize)target).Initialize();

            // initialize was called once
            Assert.AreEqual(1, target.InitializeCount);
            Assert.AreEqual(1, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);
        }

        [TestMethod]
        public void DoubleInitializeTest()
        {
            var target = new MyTarget();
            ((ISupportsInitialize)target).Initialize();
            ((ISupportsInitialize)target).Initialize();

            // initialize was called once
            Assert.AreEqual(1, target.InitializeCount);
            Assert.AreEqual(1, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);
        }

        [TestMethod]
        public void DoubleCloseTest()
        {
            var target = new MyTarget();
            ((ISupportsInitialize)target).Initialize();
            ((ISupportsInitialize)target).Close();
            ((ISupportsInitialize)target).Close();

            // initialize and close were called once each
            Assert.AreEqual(1, target.InitializeCount);
            Assert.AreEqual(1, target.CloseCount);
            Assert.AreEqual(2, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);
        }

        [TestMethod]
        public void CloseWithoutInitializeTest()
        {
            var target = new MyTarget();
            ((ISupportsInitialize)target).Close();

            // nothing was called
            Assert.AreEqual(0, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);
        }

        [TestMethod]
        public void WriteWithoutInitializeTest()
        {
            var target = new MyTarget();
            List<Exception> exceptions = new List<Exception>();
            target.WriteLogEvent(LogEventInfo.CreateNullEvent(), exceptions.Add);
            target.WriteLogEvents(new[] { LogEventInfo.CreateNullEvent(), LogEventInfo.CreateNullEvent(), LogEventInfo.CreateNullEvent() }, new AsyncContinuation[] { exceptions.Add, exceptions.Add, exceptions.Add });

            // write was not called
            Assert.AreEqual(0, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);
            Assert.AreEqual(4, exceptions.Count);
            exceptions.ForEach(Assert.IsNull);
        }

        [TestMethod]
        public void WriteOnClosedTargetTest()
        {
            var target = new MyTarget();
            ((ISupportsInitialize)target).Initialize();
            ((ISupportsInitialize)target).Close();

            List<Exception> exceptions = new List<Exception>();
            target.WriteLogEvent(LogEventInfo.CreateNullEvent(), exceptions.Add);
            target.WriteLogEvents(new[] { LogEventInfo.CreateNullEvent(), LogEventInfo.CreateNullEvent(), LogEventInfo.CreateNullEvent() }, new AsyncContinuation[] { exceptions.Add, exceptions.Add, exceptions.Add });

            Assert.AreEqual(1, target.InitializeCount);
            Assert.AreEqual(1, target.CloseCount);

            // write was not called
            Assert.AreEqual(2, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);

            // but all callbacks were invoked with null values
            Assert.AreEqual(4, exceptions.Count);
            exceptions.ForEach(Assert.IsNull);
        }

        [TestMethod]
        public void FlushTest()
        {
            var target = new MyTarget();
            List<Exception> exceptions = new List<Exception>();
            ((ISupportsInitialize)target).Initialize();
            target.Flush(exceptions.Add);

            // flush was called
            Assert.AreEqual(1, target.FlushCount);
            Assert.AreEqual(2, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);
            Assert.AreEqual(1, exceptions.Count);
            exceptions.ForEach(Assert.IsNull);
        }

        [TestMethod]
        public void FlushWithoutInitializeTest()
        {
            var target = new MyTarget();
            List<Exception> exceptions = new List<Exception>();
            target.Flush(exceptions.Add);

            Assert.AreEqual(1, exceptions.Count);
            exceptions.ForEach(Assert.IsNull);

            // flush was not called
            Assert.AreEqual(0, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);
        }

        [TestMethod]
        public void FlushOnClosedTargetTest()
        {
            var target = new MyTarget();
            ((ISupportsInitialize)target).Initialize();
            ((ISupportsInitialize)target).Close();
            Assert.AreEqual(1, target.InitializeCount);
            Assert.AreEqual(1, target.CloseCount);

            List<Exception> exceptions = new List<Exception>();
            target.Flush(exceptions.Add);

            Assert.AreEqual(1, exceptions.Count);
            exceptions.ForEach(Assert.IsNull);

            // flush was not called
            Assert.AreEqual(2, target.InitializeCount + target.FlushCount + target.CloseCount + target.WriteCount + target.WriteCount2 + target.WriteCount3);
        }

        [TestMethod]
        public void LockingTest()
        {
            var target = new MyTarget();
            ((ISupportsInitialize)target).Initialize();

            var mre = new ManualResetEvent(false);

            Exception backgroundThreadException = null;

            Thread t = new Thread(() =>
            {
                try
                {
                    target.BlockingOperation(1000);
                }
                catch (Exception ex)
                {
                    backgroundThreadException = ex;
                }
                finally
                {
                    mre.Set();
                }
            });


            ((ISupportsInitialize)target).Initialize();
            t.Start();
            Thread.Sleep(50);
            List<Exception> exceptions = new List<Exception>();
            target.WriteLogEvent(LogEventInfo.CreateNullEvent(), exceptions.Add);
            target.WriteLogEvents(new[] { LogEventInfo.CreateNullEvent(), LogEventInfo.CreateNullEvent() }, new AsyncContinuation[] { exceptions.Add, exceptions.Add });
            target.Flush(exceptions.Add);
            ((ISupportsInitialize)target).Close();

            exceptions.ForEach(Assert.IsNull);

            mre.WaitOne();
            if (backgroundThreadException != null)
            {
                Assert.Fail(backgroundThreadException.ToString());
            }
        }

        public class MyTarget : Target
        {
            private int inBlockingOperation;

            public int InitializeCount { get; set; }
            public int CloseCount { get; set; }
            public int FlushCount { get; set; }
            public int WriteCount { get; set; }
            public int WriteCount2 { get; set; }
            public int WriteCount3 { get; set; }

            protected override void Initialize()
            {
                Assert.AreEqual(0, this.inBlockingOperation);
                this.InitializeCount++;
                base.Initialize();
            }

            protected override void Close()
            {
                Assert.AreEqual(0, this.inBlockingOperation);
                this.CloseCount++;
                base.Close();
            }

            protected override void FlushAsync(Internal.AsyncContinuation asyncContinuation)
            {
                Assert.AreEqual(0, this.inBlockingOperation);
                this.FlushCount++;
                base.FlushAsync(asyncContinuation);
            }

            protected override void Write(LogEventInfo logEvent)
            {
                Assert.AreEqual(0, this.inBlockingOperation);
                this.WriteCount++;
            }

            protected override void Write(LogEventInfo logEvent, AsyncContinuation asyncContinuation)
            {
                Assert.AreEqual(0, this.inBlockingOperation);
                this.WriteCount2++;
                base.Write(logEvent, asyncContinuation);
            }

            protected override void Write(LogEventInfo[] logEvents, AsyncContinuation[] asyncContinuations)
            {
                Assert.AreEqual(0, this.inBlockingOperation);
                this.WriteCount3++;
                base.Write(logEvents, asyncContinuations);
            }

            public void BlockingOperation(int millisecondsTimeout)
            {
                lock (this.SyncRoot)
                {
                    this.inBlockingOperation++;
                    Thread.Sleep(millisecondsTimeout);
                    this.inBlockingOperation--;
                }
            }
        }
    }
}