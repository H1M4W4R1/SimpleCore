using System;
using NUnit.Framework;
using Systems.SimpleCore.Saving.Abstract;
using Systems.SimpleCore.Saving.Abstract.Transitions;
using Systems.SimpleCore.Saving.Data.Transitions;
using Systems.SimpleCore.Saving.Utility;
using UnityEngine;
using UnityEngine.TestTools;

namespace Systems.SimpleCore.Tests
{
    public sealed class SaveAPITests
    {
        [SetUp]
        public void SetUp()
        {
            SaveAPI.RebuildAdjacencyMap();
        }

        [Test]
        public void Save_UsesDefaultSaveFileWhenProviderDeclaresOne()
        {
            DualFormatSaveData saveData = new DualFormatSaveData(3, "Ari", 99);

            SaveFileBase saveFile = SaveAPI.Save(saveData);

            CoreSaveFileV2 version2 = saveFile as CoreSaveFileV2;
            Assert.IsNotNull(version2);
            Assert.AreEqual(3, version2.level);
            Assert.AreEqual("Ari", version2.name);
            Assert.AreEqual(99, version2.score);
            Assert.AreEqual(1, saveData.collectCalls);
        }

        [Test]
        public void SaveAs_UsesDirectSupportedTypeWithoutConversion()
        {
            DualFormatSaveData saveData = new DualFormatSaveData(4, "Bea", 44);

            SaveFileBase saveFile = SaveAPI.SaveAs<CoreSaveFileV1>(saveData);

            CoreSaveFileV1 version1 = saveFile as CoreSaveFileV1;
            Assert.IsNotNull(version1);
            Assert.AreEqual(4, version1.level);
            Assert.AreEqual("Bea", version1.name);
            Assert.AreEqual(1, saveData.collectCalls);
        }

        [Test]
        public void SaveAs_ConvertsThroughShortestUpgradePath()
        {
            V1OnlySaveData saveData = new V1OnlySaveData(5, "Cai");

            SaveFileBase saveFile = SaveAPI.SaveAs<CoreSaveFileV3>(saveData);

            CoreSaveFileV3 version3 = saveFile as CoreSaveFileV3;
            Assert.IsNotNull(version3);
            Assert.AreEqual(5, version3.level);
            Assert.AreEqual("Cai", version3.name);
            Assert.AreEqual(50, version3.score);
            Assert.AreEqual("Cai:5", version3.summary);
        }

        [Test]
        public void Load_ConvertsIncomingFileToSupportedType()
        {
            V3OnlySaveData saveData = new V3OnlySaveData();
            CoreSaveFileV1 version1 = new CoreSaveFileV1
            {
                level = 7,
                name = "Dee"
            };

            SaveAPI.Load(saveData, version1);

            Assert.AreEqual(7, saveData.level);
            Assert.AreEqual("Dee", saveData.name);
            Assert.AreEqual(70, saveData.score);
            Assert.AreEqual("Dee:7", saveData.summary);
            Assert.AreEqual(1, saveData.distributeCalls);
        }

        [Test]
        public void Load_UsesActualRuntimeTypeWhenProvidedTypeDoesNotMatchFile()
        {
            V3OnlySaveData saveData = new V3OnlySaveData();
            CoreSaveFileV3 version3 = new CoreSaveFileV3
            {
                level = 8,
                name = "Eli",
                score = 800,
                summary = "ready"
            };

            SaveAPI.Load(saveData, version3, typeof(CoreSaveFileV2));

            Assert.AreEqual(8, saveData.level);
            Assert.AreEqual("Eli", saveData.name);
            Assert.AreEqual(800, saveData.score);
            Assert.AreEqual("ready", saveData.summary);
        }

        [Test]
        public void Load_CanDowngradeIncomingFileToSupportedType()
        {
            V1OnlySaveData saveData = new V1OnlySaveData(0, string.Empty);
            CoreSaveFileV3 version3 = new CoreSaveFileV3
            {
                level = 9,
                name = "Fox",
                score = 900,
                summary = "summary"
            };

            SaveAPI.Load(saveData, version3);

            Assert.AreEqual(9, saveData.level);
            Assert.AreEqual("Fox", saveData.name);
        }

        [Test]
        public void SaveAs_ReturnsNullAndLogsWhenConversionPathDoesNotExist()
        {
            V1OnlySaveData saveData = new V1OnlySaveData(1, "NoPath");

            LogAssert.Expect(LogType.Error, "No conversion path found from any supported save-file types [CoreSaveFileV1] to requested UnreachableSaveFile.");
            SaveFileBase saveFile = SaveAPI.SaveAs<UnreachableSaveFile>(saveData);

            Assert.IsNull(saveFile);
        }

        [Test]
        public void ComputeTransitionPath_ReturnsNoOpForSameType()
        {
            TransitionInfo transitionInfo = SaveAPI.ComputeTransitionPath<CoreSaveFileV1, CoreSaveFileV1>();

            Assert.IsTrue(transitionInfo.IsPossible);
            Assert.AreEqual(typeof(CoreSaveFileV1), transitionInfo.From);
            Assert.AreEqual(typeof(CoreSaveFileV1), transitionInfo.To);
            Assert.AreEqual(0, transitionInfo.Steps.Count);
            StringAssert.Contains("No-op transition", transitionInfo.ToString());
        }

        [Test]
        public void ComputeTransitionPath_FindsMultiStepUpgradePath()
        {
            TransitionInfo transitionInfo = SaveAPI.ComputeTransitionPath<CoreSaveFileV1, CoreSaveFileV3>();

            Assert.IsTrue(transitionInfo.IsPossible);
            Assert.AreEqual(2, transitionInfo.Steps.Count);
            Assert.AreEqual(typeof(CoreSaveFileV1), transitionInfo.Steps[0].From);
            Assert.AreEqual(typeof(CoreSaveFileV2), transitionInfo.Steps[0].To);
            Assert.AreEqual(SaveFileTransitionKind.Upgrade, transitionInfo.Steps[0].Kind);
            Assert.AreEqual(typeof(CoreSaveFileV2), transitionInfo.Steps[1].From);
            Assert.AreEqual(typeof(CoreSaveFileV3), transitionInfo.Steps[1].To);
            Assert.AreEqual(SaveFileTransitionKind.Upgrade, transitionInfo.Steps[1].Kind);
        }

        [Test]
        public void ComputeTransitionPath_FindsMultiStepDowngradePath()
        {
            TransitionInfo transitionInfo = SaveAPI.ComputeTransitionPath<CoreSaveFileV3, CoreSaveFileV1>();

            Assert.IsTrue(transitionInfo.IsPossible);
            Assert.AreEqual(2, transitionInfo.Steps.Count);
            Assert.AreEqual(SaveFileTransitionKind.Downgrade, transitionInfo.Steps[0].Kind);
            Assert.AreEqual(SaveFileTransitionKind.Downgrade, transitionInfo.Steps[1].Kind);
        }

        [Test]
        public void SaveFileTransitionStep_RejectsNullTypes()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SaveFileTransitionStep ignored = new SaveFileTransitionStep(null, typeof(CoreSaveFileV1), SaveFileTransitionKind.Upgrade);
                Assert.AreEqual(default(SaveFileTransitionStep), ignored);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                SaveFileTransitionStep ignored = new SaveFileTransitionStep(typeof(CoreSaveFileV1), null, SaveFileTransitionKind.Upgrade);
                Assert.AreEqual(default(SaveFileTransitionStep), ignored);
            });
        }

        [Serializable]
        private sealed class CoreSaveFileV1 : SaveFileBase, IUpgradeableSaveFile<CoreSaveFileV2, CoreSaveFileV1>
        {
            public int level;
            public string name;

            public CoreSaveFileV2 GetUpgradedVersion(CoreSaveFileV1 originalFile)
            {
                return new CoreSaveFileV2
                {
                    level = originalFile.level,
                    name = originalFile.name,
                    score = originalFile.level * 10
                };
            }
        }

        [Serializable]
        private sealed class CoreSaveFileV2 :
            SaveFileBase,
            IUpgradeableSaveFile<CoreSaveFileV3, CoreSaveFileV2>,
            IDowngradableSaveFile<CoreSaveFileV1, CoreSaveFileV2>
        {
            public int level;
            public string name;
            public int score;

            public CoreSaveFileV3 GetUpgradedVersion(CoreSaveFileV2 originalFile)
            {
                return new CoreSaveFileV3
                {
                    level = originalFile.level,
                    name = originalFile.name,
                    score = originalFile.score,
                    summary = originalFile.name + ":" + originalFile.level
                };
            }

            public CoreSaveFileV1 GetDowngradedVersion(CoreSaveFileV2 originalFile)
            {
                return new CoreSaveFileV1
                {
                    level = originalFile.level,
                    name = originalFile.name
                };
            }
        }

        [Serializable]
        private sealed class CoreSaveFileV3 :
            SaveFileBase,
            IDowngradableSaveFile<CoreSaveFileV2, CoreSaveFileV3>
        {
            public int level;
            public string name;
            public int score;
            public string summary;

            public CoreSaveFileV2 GetDowngradedVersion(CoreSaveFileV3 originalFile)
            {
                return new CoreSaveFileV2
                {
                    level = originalFile.level,
                    name = originalFile.name,
                    score = originalFile.score
                };
            }
        }

        [Serializable]
        private sealed class UnreachableSaveFile : SaveFileBase
        {
            public int value;
        }

        private sealed class DualFormatSaveData :
            ISaveData<CoreSaveFileV1>,
            ISaveData<CoreSaveFileV2>,
            IHasDefaultSaveFile
        {
            private readonly int _level;
            private readonly string _name;
            private readonly int _score;

            public int collectCalls;

            public DualFormatSaveData(int level, string name, int score)
            {
                _level = level;
                _name = name;
                _score = score;
            }

            public Type DefaultSaveFileType => typeof(CoreSaveFileV2);

            public void CollectData()
            {
                collectCalls++;
            }

            public CoreSaveFileV1 BuildSaveFile()
            {
                return new CoreSaveFileV1
                {
                    level = _level,
                    name = _name
                };
            }

            public void ParseSaveFile(CoreSaveFileV1 saveFile)
            {
            }

            CoreSaveFileV2 ISaveData<CoreSaveFileV2>.BuildSaveFile()
            {
                return new CoreSaveFileV2
                {
                    level = _level,
                    name = _name,
                    score = _score
                };
            }

            void ISaveData<CoreSaveFileV2>.ParseSaveFile(CoreSaveFileV2 saveFile)
            {
            }
        }

        private sealed class V1OnlySaveData : ISaveData<CoreSaveFileV1>
        {
            public int level;
            public string name;

            public V1OnlySaveData(int level, string name)
            {
                this.level = level;
                this.name = name;
            }

            public void CollectData()
            {
            }

            public CoreSaveFileV1 BuildSaveFile()
            {
                return new CoreSaveFileV1
                {
                    level = level,
                    name = name
                };
            }

            public void ParseSaveFile(CoreSaveFileV1 saveFile)
            {
                level = saveFile.level;
                name = saveFile.name;
            }
        }

        private sealed class V3OnlySaveData : ISaveData<CoreSaveFileV3>
        {
            public int level;
            public string name;
            public int score;
            public string summary;
            public int distributeCalls;

            public void CollectData()
            {
            }

            public void DistributeData()
            {
                distributeCalls++;
            }

            public CoreSaveFileV3 BuildSaveFile()
            {
                return new CoreSaveFileV3
                {
                    level = level,
                    name = name,
                    score = score,
                    summary = summary
                };
            }

            public void ParseSaveFile(CoreSaveFileV3 saveFile)
            {
                level = saveFile.level;
                name = saveFile.name;
                score = saveFile.score;
                summary = saveFile.summary;
            }
        }
    }
}
