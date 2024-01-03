﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ME.ECS {

    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public abstract class InitializerBase : MonoBehaviour {

        public enum ConfigurationType {
            DebugOnly,
            ReleaseOnly,
            DebugAndRelease,
        }

        public enum CodeSize {

            Unknown = 0,
            /// <summary>
            /// Has static size without generics
            /// </summary>
            Light,
            /// <summary>
            /// Depends on components count, but contains not heavy/doesn't contains any generic instructions
            /// </summary>
            Normal,
            /// <summary>
            /// Depends on components count and contains heavy generic instructions
            /// </summary>
            Heavy,

        }

        public enum RuntimeSpeed {

            Unknown = 0,
            /// <summary>
            /// Has no additional instructions at all at runtime 
            /// </summary>
            Light,
            /// <summary>
            /// Has additional light-weight instructions at runtime
            /// </summary>
            Normal,
            /// <summary>
            /// Has heavy instructions every tick
            /// </summary>
            Heavy,

        }

        public struct DefineInfo {

            public string define;
            public string description;
            public System.Func<bool> isActive;
            public bool showInList;
            public ConfigurationType configurationType;
            public CodeSize codeSize;
            public RuntimeSpeed runtimeSpeed;
            public bool actualValue;
            public string deprecatedVersion;

            public DefineInfo(bool actualValue, string define, string description, System.Func<bool> isActive, bool showInList, ConfigurationType configurationType, CodeSize codeSize, RuntimeSpeed runtimeSpeed, string deprecatedVersion = null) {

                this.actualValue = actualValue;
                this.define = define;
                this.description = description;
                this.isActive = isActive;
                this.showInList = showInList;
                this.configurationType = configurationType;
                this.codeSize = codeSize;
                this.runtimeSpeed = runtimeSpeed;
                this.deprecatedVersion = deprecatedVersion;

            }

        }

        [System.Serializable]
        public struct Configuration {

            [System.Serializable]
            public struct Define {

                public bool enabled;
                public string name;

                public bool IsActualEnabled(DefineInfo defineInfo) {

                    return this.enabled == defineInfo.actualValue;

                }

            }

            public string name;
            public bool isDirty;
            public ConfigurationType configurationType;
            public System.Collections.Generic.List<Define> defines;

            public bool Contains(DefineInfo info) {

                if (defines == null)
                {
                    return false;
                }

                foreach (var item in this.defines) {
                    if (item.name == info.define) {
                        return true;
                    }
                }

                return false;

            }

            public void Remove(DefineInfo info) {

                for (int i = 0; i < this.defines.Count; ++i) {

                    if (this.defines[i].name == info.define) {
                        this.defines.RemoveAt(i);
                        return;
                    }
                    
                }
                
            }
            
            public bool Add(DefineInfo info) {

                if (this.configurationType == ConfigurationType.DebugOnly &&
                    info.configurationType == InitializerBase.ConfigurationType.ReleaseOnly) {

                    return false;

                }

                if (this.configurationType == ConfigurationType.ReleaseOnly &&
                    info.configurationType == InitializerBase.ConfigurationType.DebugOnly) {
                    
                    return false;
                    
                }

                if (this.defines == null) this.defines = new System.Collections.Generic.List<Define>();
                var isExists = false;
                foreach (var item in this.defines) {

                    if (item.name == info.define) {

                        isExists = true;
                        break;

                    }
                    
                }

                if (isExists == false) {
                    
                    this.defines.Add(new Define() {
                        enabled = info.isActive.Invoke(),
                        name = info.define,
                    });
                    return true;
                    
                }

                return false;

            }

            public void SetEnabled(string define) {

                for (var i = 0; i < this.defines.Count; ++i) {
                    
                    var item = this.defines[i];
                    if (item.name == define) {

                        item.enabled = true;
                        this.defines[i] = item;
                        break;

                    }
                }

            }

            public void SetDisabled(string define) {

                for (var i = 0; i < this.defines.Count; ++i) {
                    
                    var item = this.defines[i];
                    if (item.name == define) {

                        item.enabled = false;
                        this.defines[i] = item;
                        break;

                    }
                }

            }

        }

        [System.Serializable]
        public struct EndOfBaseClass { }
        
        public System.Collections.Generic.List<Configuration> configurations = new System.Collections.Generic.List<Configuration>();
        public string selectedConfiguration;

        public FeaturesListCategories featuresListCategories = new FeaturesListCategories();
        public WorldSettings worldSettings = WorldSettings.Default;
        public WorldDebugSettings worldDebugSettings = WorldDebugSettings.Default;
        public EndOfBaseClass endOfBaseClass;

        public delegate void InitializeSceneCallback(World world, bool callLateInitialization);
        private static InitializeSceneCallback initializeSceneCallback;
        
        public static void RegisterSceneCallback(InitializeSceneCallback initializeSceneCallback) {

            InitializerBase.initializeSceneCallback += initializeSceneCallback;

        }

        protected void Initialize(World world, bool callLateInitialization = true) {

            world.SetSettings(this.worldSettings);
            world.SetDebugSettings(this.worldDebugSettings);
            world.TryInitializeDefaults();

            // Initialize features
            this.InitializeFeatures(world, callLateInitialization);
            
            // Initialize scene
            InitializerBase.initializeSceneCallback?.Invoke(world, callLateInitialization);

        }

        protected void InitializeFeatures(World world, bool callLateInitialization) {

            this.featuresListCategories.Initialize(world, callLateInitialization);

        }

        protected void DeInitializeFeatures(World world) {

            this.featuresListCategories.DeInitialize(world);

        }

    }

}
