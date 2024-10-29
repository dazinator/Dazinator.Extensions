namespace Dazinator.Extensions.DependencyInjection.Tests.Child
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Dazinator.Extensions.DependencyInjection.ChildContainers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    public class ChildServiceCollectionTests
    {

        [Fact]
        public void Can_Construct()
        {
            IServiceCollection parentServiceCollection = new ServiceCollection();
            var sut = new ChildServiceCollection(parentServiceCollection);
            Assert.NotNull(sut);
        }

        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void Add_AddsToChildDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
                Assert.Contains(sut.GetChildServiceDescriptors(), (a) => a.ServiceType == childType);
            }

            Assert.Equal(childServices.Count(), sut.GetChildServiceDescriptors().Count());
        }

        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0], 1)]
        [InlineData(new Type[0], new[] { typeof(AnimalService) }, 1)]
        [InlineData(new Type[0], new Type[0], 0)]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(AnimalService) }, 2)]
        public void Count_Returns_SumOfParentAndChildDescriptors(Type[] parentServices, Type[] childServices, int expectedCount)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var item in parentServices)
            {
                parentServiceCollection.AddTransient(item);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var item in childServices)
            {
                sut.AddTransient(item);
            }

            var count = sut.Count;
            Assert.Equal(expectedCount, count);
        }


        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void Indexer_Get_ReturnsDescriptor(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var testIndex = 0;
            for (var i = 0; i < parentServiceCollection.Count; i++)
            {
                var parentItem = parentServiceCollection[testIndex];
                var testItem = sut[testIndex];

                Assert.NotNull(testItem);
                Assert.Equal(parentItem, testItem);

                testIndex += 1;
            }

            var totalItems = parentServiceCollection.Count + childServices.Count();
            for (var i = testIndex; i < totalItems; i++)
            {
                var childIndex = testIndex - parentServiceCollection.Count;
                var childType = childServices[childIndex];

                var testItem = sut[testIndex];
                Assert.NotNull(testItem);
                Assert.Equal(childType, testItem.ServiceType);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => sut[testIndex + 1]);

        }

        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void Indexer_Set_SetsOnlyChildDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var testIndex = 0;
            for (var i = 0; i < parentServiceCollection.Count; i++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sut[testIndex] = null);
                testIndex += 1;
            }

            var totalItems = parentServiceCollection.Count + childServices.Count();
            for (var i = testIndex; i < totalItems; i++)
            {
                var childIndex = testIndex - parentServiceCollection.Count;
                var childType = childServices[childIndex];
                sut[testIndex] = null;
                Assert.Null(sut[testIndex]);

            }

            Assert.Throws<ArgumentOutOfRangeException>(() => sut[testIndex + 1] = null);

        }


        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void Clear_ClearsOnlyChildDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            sut.Clear();
            Assert.Equal(parentServiceCollection.Count, sut.Count);
        }

        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void Contains_Returns_ChildOrParentContains(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            foreach (var item in parentServiceCollection)
            {
                Assert.Contains(item, sut);
            }

            Assert.Equal(childServices.Count(), sut.GetChildServiceDescriptors().Count());
        }

        [Fact]
        public void IsReadOnly_Returns_False()
        {
            var parentServiceCollection = new ServiceCollection();
            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            Assert.False(sut.IsReadOnly);
        }

        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0], 3)]
        [InlineData(new Type[0], new[] { typeof(AnimalService) }, 4)]
        [InlineData(new Type[0], new Type[0], 5)]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) }, 6)]
        public void CopyTo_CopiesAllDescriptors(Type[] parentServices, Type[] childServices, int offSet = 0)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var totalItemCount = parentServices.Count() + childServices.Count();
            var arraySize = offSet + totalItemCount;

            var allItemsArray = new ServiceDescriptor[arraySize];
            sut.CopyTo(allItemsArray, offSet);

            for (var i = 0; i < totalItemCount; i++)
            {
                var item = allItemsArray[i + offSet];
                Assert.NotNull(item);
            }
        }

        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void Remove_OnlyRemovesChildDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var testIndex = 0;
            for (var i = 0; i < parentServiceCollection.Count; i++)
            {
                var item = sut[i];
                Assert.False(sut.Remove(item));
                testIndex += 1;
            }

            var totalItems = parentServiceCollection.Count + childServices.Count();
            for (var i = testIndex; i < totalItems; i++)
            {
                var oldCount = sut.Count;
                var item = sut[i];
                Assert.NotNull(item);
                sut.Remove(item);
                Assert.Equal(oldCount - 1, sut.Count);
            }

            Assert.False(sut.GetChildServiceDescriptors().Any());
            Assert.Equal(parentServiceCollection.Count, sut.Count);
        }


        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void RemoveAt_ReadOnlyParent_ThrowsRemovingParentDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone(), allowModifyingParentLevelServices: false);
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var testIndex = 0;
            for (var i = 0; i < parentServiceCollection.Count; i++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sut.RemoveAt(i));
                testIndex += 1;
            }

            var originalCount = sut.Count;

            var totalItems = parentServiceCollection.Count + childServices.Count();
            for (var i = testIndex; i < totalItems; i++)
            {
                var oldCount = sut.Count;
                sut.RemoveAt(i);
                Assert.Equal(oldCount - 1, sut.Count);
            }

            Assert.False(sut.GetChildServiceDescriptors().Any());
            Assert.Equal(parentServiceCollection.Count, sut.Count);
        }


        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void Insert_OnlyInsertsChildDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var testIndex = 0;
            for (var i = 0; i < parentServiceCollection.Count; i++)
            {
                var descriptor = sut[i];
                Assert.Throws<ArgumentOutOfRangeException>(() => sut.Insert(i, descriptor));
                testIndex += 1;
            }

            var originalCount = sut.Count;

            // insert item at index after parent items (i.e first child service descriptor)
            var newDescriptor = new ServiceDescriptor(typeof(AnimalService), typeof(AnimalService), ServiceLifetime.Scoped);
            sut.Insert(testIndex, newDescriptor);
            Assert.Equal(originalCount + 1, sut.Count);
            Assert.Same(sut[testIndex], newDescriptor);
            Assert.Equal(originalCount - parentServiceCollection.Count + 1, sut.GetChildServiceDescriptors().Count());

            // insert item at bottom of child descriptors
            var bottomIndex = sut.Count;
            sut.Insert(bottomIndex, newDescriptor);
            Assert.Same(sut[bottomIndex], newDescriptor);
            Assert.Same(sut.Last(), newDescriptor);

            // try and insert past bottom shoudl throw
            Assert.Throws<ArgumentOutOfRangeException>(() => sut.Insert(sut.Count + 1, newDescriptor));
        }


        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void GetEnumerator_EnumeratesAllDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var expectedTotalItems = parentServiceCollection.Count + childServices.Count();
            var itemCount = 0;
            foreach (var item in sut)
            {
                Assert.NotNull(item);
                itemCount += 1;
            }

            Assert.Equal(expectedTotalItems, itemCount);
        }

        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void IndexOf_ReturnsIndex_AccrossParentAndChildDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var totalItems = parentServiceCollection.Count + childServices.Count();
            for (var i = 0; i < totalItems; i++)
            {
                var item = sut[i];
                var itemIndex = sut.IndexOf(item);
                Assert.Equal(i, itemIndex);
            }
        }


        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void ChildDescriptors_OnlyReturnsChildNotParentDescriptors(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.Clone());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var children = sut.GetChildServiceDescriptors().ToList();
            Assert.Equal(childServices.Count(), children.Count);

            foreach (var item in childServices)
            {
                Assert.Contains(children, (i) => i.ServiceType == item);
            }
        }


        [Fact]
        public void AutoPromote_AllowsDuplicatedDescriptorsToBe_PromotedToChildOnlyLevel()
        {

            // In this scenario, a service is registered in the parent,
            // and we pretend we have an external AddXyz() method we want to call on the child IServiceCollection,
            // and that AddXyz() has some logic that will TryAdd() to add the same service descriptor.
            // because the service descriptor has already been added at parent level this would shouldy result in a duplicate NOT
            // being added as the TryAdd() will fail at child level as the child sees ALL of the parent registrations.
            // Therefore we allow the user to filter the parent registrations from view, so that AddXyz doesn't see any registrations
            // at parent level, and therefore adds all of its services again at parent level.

            var parentServiceCollection = new ServiceCollection();
            parentServiceCollection.AddSingleton<LionService>();
            parentServiceCollection.AddTransient<AnimalService>();

            Assert.Equal(2, parentServiceCollection.Count);


            IChildServiceCollection sut = new ChildServiceCollection(parentServiceCollection.Clone());
            Assert.Equal(2, sut.Count);
            Assert.Equal(2, sut.GetParentServiceDescriptors().Count());
            Assert.Empty(sut.GetChildServiceDescriptors());

            // demonstrates the issue - this will not add any service because the service descriptor already visible at parent level,
            sut.TryAddSingleton<LionService>();
            Assert.Equal(2, sut.Count);
            Assert.Equal(2, sut.GetParentServiceDescriptors().Count());
            Assert.Empty(sut.GetChildServiceDescriptors());

            // Within the action below, we are hiding parent level service descriptors that match the predicate,
            // vausing them to be added again by TryAdd()
            sut = sut.AutoDuplicateSingletons((nested) =>
             {
                 // Singleton LionService should be hidden, so TryAdd() calls should succeed
                 Assert.Single(nested);
                 Assert.Single(nested.GetParentServiceDescriptors());
                 Assert.Empty(nested.GetChildServiceDescriptors());

                 nested.TryAddSingleton<LionService>();
                 Assert.Equal(2, nested.Count);
                 Assert.Single(nested.GetParentServiceDescriptors());
                 Assert.Single(nested.GetChildServiceDescriptors());
             });

            // The duplicated service descriptor should no longer be in the parent services, only in child services - it has been promoted.
            Assert.Equal(2, sut.Count);
            Assert.Single(sut.GetParentServiceDescriptors());
            Assert.Single(sut.GetChildServiceDescriptors());
        }

    }


    public class AnimalService
    {
        public string? SomeProperty { get; set; }
    }

    public class LionService : AnimalService
    {
        public bool SomeOtherProperty { get; set; }
    }

}
