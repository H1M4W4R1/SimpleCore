using System.Collections.Generic;
using NUnit.Framework;
using Systems.SimpleCore.Identifiers;
using Systems.SimpleCore.Storage.Databases;
using Systems.SimpleCore.Storage.Lists;
using UnityEngine;

namespace Systems.SimpleCore.Tests
{
    public sealed class StorageTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int objectIndex = 0; objectIndex < _createdObjects.Count; objectIndex++)
            {
                Object createdObject = _createdObjects[objectIndex];
                if (ReferenceEquals(createdObject, null)) continue;
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void RWListAccess_CreateProvidesMutableListAndReleaseInvalidatesAccess()
        {
            RWListAccess<int> access = RWListAccess<int>.Create();

            Assert.IsTrue(access.IsValid);
            access.List.Add(10);
            access.List.Add(20);
            Assert.AreEqual(2, access.List.Count);

            access.Release();

            Assert.IsFalse(access.IsValid);
        }

        [Test]
        public void ROListAccess_CreateProvidesReadOnlyViewAndReleaseReturnsPooledList()
        {
            ROListAccess<string> access = ROListAccess<string>.Create();

            Assert.AreEqual(0, access.List.Count);

            access.Release();
        }

        [Test]
        public void AddressableDatabaseEntry_StoresHashAndObject()
        {
            DatabaseLeafAsset asset = CreateAsset<DatabaseLeafAsset>("entry");
            HashIdentifier hashIdentifier = HashIdentifier.New(typeof(DatabaseLeafAsset));

            AddressableDatabaseEntry<DatabaseBaseAsset> entry =
                new AddressableDatabaseEntry<DatabaseBaseAsset>(hashIdentifier, asset);

            Assert.AreEqual(hashIdentifier, entry.hashIdentifier);
            Assert.AreSame(asset, entry.entryObject);
        }

        [Test]
        public void RWListAccess_ToReadOnlyExposesSameCurrentItems()
        {
            RWListAccess<int> access = RWListAccess<int>.Create();
            access.List.Add(4);
            access.List.Add(8);

            ROListAccess<int> readOnlyAccess = access.ToReadOnly();
            try
            {
                Assert.AreEqual(2, readOnlyAccess.List.Count);
                Assert.AreEqual(4, readOnlyAccess.List[0]);
                Assert.AreEqual(8, readOnlyAccess.List[1]);
            }
            finally
            {
                readOnlyAccess.Release();
            }
        }

        [Test]
        public void RWListAccess_CreateClearsListReturnedFromPool()
        {
            RWListAccess<int> firstAccess = RWListAccess<int>.Create();
            firstAccess.List.Add(12);
            firstAccess.Release();

            RWListAccess<int> secondAccess = RWListAccess<int>.Create();
            try
            {
                Assert.AreEqual(0, secondAccess.List.Count);
            }
            finally
            {
                secondAccess.Release();
            }
        }

        private TAsset CreateAsset<TAsset>(string assetName)
            where TAsset : DatabaseBaseAsset
        {
            TAsset asset = ScriptableObject.CreateInstance<TAsset>();
            asset.name = assetName;
            _createdObjects.Add(asset);
            return asset;
        }

        public class DatabaseBaseAsset : ScriptableObject
        {
        }

        public class DatabaseIntermediateAsset : DatabaseBaseAsset
        {
        }

        public sealed class DatabaseLeafAsset : DatabaseIntermediateAsset
        {
        }

        public sealed class DatabaseOtherLeafAsset : DatabaseBaseAsset
        {
        }
    }
}
