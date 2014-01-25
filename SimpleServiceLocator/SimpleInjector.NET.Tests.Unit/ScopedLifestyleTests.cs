﻿namespace SimpleInjector.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ScopedLifestyleTests
    {
        [TestMethod]
        public void Dispose_ListWithOneItem_DisposesItem()
        {
            // Arrange
            var scope = new Scope();

            var disposable = new DisposableObject();

            var disposables = new List<IDisposable> { disposable };

            scope.RegisterForDisposal(disposable);
            
            // Act
            scope.Dispose();

            // Assert
            Assert.IsTrue(disposable.IsDisposedOnce);
        }
        
        [TestMethod]
        public void Dispose_MultipleItems_DisposesAllItems()
        {
            // Arrange
            var scope = new Scope();

            var disposables = new List<DisposableObject> 
            { 
                new DisposableObject(),
                new DisposableObject(),
                new DisposableObject()
            };

            disposables.ForEach(scope.RegisterForDisposal);

            // Act
            scope.Dispose();

            // Assert
            Assert.IsTrue(disposables.All(d => d.IsDisposedOnce));
        }

        [TestMethod]
        public void Dispose_MultipleItems_DisposesAllItemsInReversedOrder()
        {
            // Arrange
            var scope = new Scope();

            var disposedItems = new List<DisposableObject>();

            var disposables = new List<DisposableObject> 
            { 
                new DisposableObject(disposedItems.Add),
                new DisposableObject(disposedItems.Add),
                new DisposableObject(disposedItems.Add)
            };

            disposables.ForEach(scope.RegisterForDisposal);

            // Act
            scope.Dispose();

            disposedItems.Reverse();

            // Assert
            Assert.IsTrue(disposedItems.SequenceEqual(disposables));
        }

        [TestMethod]
        public void Dispose_WithMultipleItemsThatThrow_StillDisposesAllItems()
        {
            // Arrange
            var scope = new Scope();

            var disposables = new List<DisposableObject> 
            { 
                new DisposableObject(new Exception()),
                new DisposableObject(new Exception()),
                new DisposableObject(new Exception())
            };

            disposables.ForEach(scope.RegisterForDisposal);

            try
            {
                // Act
                scope.Dispose();

                // Assert
                Assert.Fail("Exception was expected to bubble up.");
            }
            catch
            {
                Assert.IsTrue(disposables.All(d => d.IsDisposedOnce));
            }
        }

        [TestMethod]
        public void Dispose_WithMultipleItemsThatThrow_WillBubbleUpTheLastThrownException()
        {
            // Arrange
            var scope = new Scope();

            Exception lastThrownException = new Exception();

            var disposables = new List<DisposableObject> 
            { 
                // Since the objects are disposed in reverse order, the first object is disposed last, and
                // this exception is expected to bubble up.
                new DisposableObject(exceptionToThrow: lastThrownException),
                new DisposableObject(exceptionToThrow: new Exception()),
                new DisposableObject(exceptionToThrow: new Exception()),
                new DisposableObject(),
                new DisposableObject(exceptionToThrow: new Exception()),
                new DisposableObject(exceptionToThrow: new Exception())
            };

            disposables.ForEach(scope.RegisterForDisposal);

            try
            {
                // Act
                scope.Dispose();

                // Assert
                Assert.Fail("Exception was expected to bubble up.");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(object.ReferenceEquals(lastThrownException, ex));
            }
        }

        [TestMethod]
        public void Dispose_RegisteredActionAndRegisteredDisposableObject_CallsActionFirst()
        {
            // Arrange
            int actionCount = 0;
            int disposeCount = 0;

            var scope = new Scope();
            
            scope.RegisterForDisposal(new DisposableObject(_ =>
            {
                Assert.AreEqual(2, actionCount);
                disposeCount++;
            })); 
            
            scope.WhenScopeEnds(() =>
            {
                Assert.AreEqual(0, disposeCount);
                actionCount++;
            });

            scope.WhenScopeEnds(() =>
            {
                Assert.AreEqual(0, disposeCount);
                actionCount++;
            });
            
            scope.RegisterForDisposal(new DisposableObject(_ =>
            {
                Assert.AreEqual(2, actionCount);
                disposeCount++;
            })); 
            
            // Act
            scope.Dispose();

            // Assert
            Assert.AreEqual(2, actionCount);
            Assert.AreEqual(2, disposeCount);
        }

        [TestMethod]
        public void Dispose_WithThrowingAction_StillDisposesInstance()
        {
            // Arrange
            bool disposed = false;

            var scope = new Scope();

            scope.WhenScopeEnds(() =>
            {
                throw new Exception();
            });

            scope.RegisterForDisposal(new DisposableObject(_ =>
            {
                disposed = true;
            }));

            try
            {
                // Act
                scope.Dispose();

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch
            {
                Assert.IsTrue(disposed);
            }
        }

        [TestMethod]
        public void DisposeInstances_WithNullArgument_ThrowsExpectedException()
        {
            // Act
            Action action = () => ScopedLifestyle.DisposeInstances(null);

            // Assert
            AssertThat.ThrowsWithParamName<ArgumentNullException>("disposables", action);
        }

        [TestMethod]
        public void DisposeInstances_ListWithOneItem_DisposesItem()
        {
            // Arrange
            var disposable = new DisposableObject();

            var disposables = new List<IDisposable> { disposable };

            // Act
            ScopedLifestyle.DisposeInstances(disposables);

            // Assert
            Assert.AreEqual(1, disposable.DisposeCount);
        }

        [TestMethod]
        public void DisposeInstances_ListWithMultipleItems_DisposesAllItems()
        {
            // Arrange
            var disposables = new List<DisposableObject> 
            { 
                new DisposableObject(),
                new DisposableObject(),
                new DisposableObject()
            };

            // Act
            ScopedLifestyle.DisposeInstances(disposables.Cast<IDisposable>().ToList());

            // Assert
            Assert.IsTrue(disposables.All(d => d.DisposeCount == 1));
        }

        [TestMethod]
        public void DisposeInstances_ListWithMultipleItems_DisposesAllItemsInReversedOrder()
        {
            // Arrange
            var disposedItems = new List<DisposableObject>();

            var disposables = new List<DisposableObject> 
            { 
                new DisposableObject(disposedItems.Add),
                new DisposableObject(disposedItems.Add),
                new DisposableObject(disposedItems.Add)
            };

            // Act
            ScopedLifestyle.DisposeInstances(disposables.Cast<IDisposable>().ToList());

            disposedItems.Reverse();

            // Assert
            Assert.IsTrue(disposedItems.SequenceEqual(disposables));
        }

        [TestMethod]
        public void DisposeInstances_WithMultipleItemsThatThrow_StillDisposesAllItems()
        {
            // Arrange
            var disposables = new List<DisposableObject> 
            { 
                new DisposableObject(new Exception()),
                new DisposableObject(new Exception()),
                new DisposableObject(new Exception())
            };

            try
            {
                // Act
                ScopedLifestyle.DisposeInstances(disposables.Cast<IDisposable>().ToList());

                // Assert
                Assert.Fail("Exception was expected to bubble up.");
            }
            catch
            {
                Assert.IsTrue(disposables.All(d => d.DisposeCount == 1));
            }
        }

        [TestMethod]
        public void DisposeInstances_WithMultipleItemsThatThrow_WillBubbleUpTheLastThrownException()
        {
            // Arrange
            var disposables = new List<DisposableObject> 
            { 
                new DisposableObject(new Exception()),
                new DisposableObject(new Exception()),
                new DisposableObject(new Exception()),
                new DisposableObject(),
                new DisposableObject(new Exception()),
                new DisposableObject(new Exception())
            };

            try
            {
                // Act
                ScopedLifestyle.DisposeInstances(disposables.Cast<IDisposable>().ToList());

                // Assert
                Assert.Fail("Exception was expected to bubble up.");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(object.ReferenceEquals(disposables.First().ExceptionToThrow, ex));
            }
        }

        private sealed class DisposableObject : IDisposable
        {
            private Action<DisposableObject> disposing;

            public DisposableObject()
            {
            }

            public DisposableObject(Action<DisposableObject> disposing)
            {
                this.disposing = disposing;
            }

            public DisposableObject(Exception exceptionToThrow)
            {
                this.ExceptionToThrow = exceptionToThrow;
            }

            public Exception ExceptionToThrow { get; private set; }

            public int DisposeCount { get; private set; }

            public bool IsDisposedOnce 
            {
                get { return this.DisposeCount == 1; } 
            }

            public void Dispose()
            {
                this.DisposeCount++;

                if (this.disposing != null)
                {
                    this.disposing(this);
                }

                if (this.ExceptionToThrow != null)
                {
                    throw this.ExceptionToThrow;
                }
            }
        }
        
        private sealed class FakeScopedLifestyle : ScopedLifestyle
        {
            private readonly Scope scope;

            public FakeScopedLifestyle(Scope scope)
                : base("Fake Scope")
            {
                this.scope = scope;
            }

            protected internal override Func<Scope> CreateCurrentScopeProvider(Container container)
            {
                return () => this.scope;
            }

            protected override int Length
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}