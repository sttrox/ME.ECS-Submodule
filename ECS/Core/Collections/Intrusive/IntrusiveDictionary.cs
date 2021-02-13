﻿
namespace ME.ECS.Collections {

    public interface IIntrusiveDictionary<TKey, TValue> where TKey : struct, System.IEquatable<TKey> where TValue : struct {

        int Count { get; }
        TValue this[TKey key] { get; set; }
        
        bool Add(in TKey key, in TValue value);
        bool RemoveKey(in TKey key);
        bool TryGetValue(in TKey key, out TValue value);
        bool ContainsKey(in TKey key);
        void Clear();
        
        BufferArray<IntrusiveDictionary<TKey, TValue>.Entry> ToArray();
        IntrusiveDictionary<TKey, TValue>.Enumerator GetEnumerator();

    }
    
    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public struct IntrusiveDictionary<TKey, TValue> : IIntrusiveDictionary<TKey, TValue> where TKey : struct, System.IEquatable<TKey> where TValue : struct {

        #if ECS_COMPILE_IL2CPP_OPTIONS
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
        #endif
        public struct Enumerator : System.Collections.Generic.IEnumerator<Entry> {

            private IntrusiveHashSetGeneric<Entry>.Enumerator listEnumerator;
            public Entry Current => this.listEnumerator.Current;

            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public Enumerator(IntrusiveDictionary<TKey, TValue> hashSet) {

                this.listEnumerator = hashSet.keys.GetEnumerator();
                
            }

            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() {

                return this.listEnumerator.MoveNext();

            }

            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Reset() {
                
                this.listEnumerator = default;

            }

            object System.Collections.IEnumerator.Current {
                get {
                    throw new AllocationException();
                }
            }

            public void Dispose() {

            }

        }

        public struct Entry : System.IEquatable<Entry> {

            public TKey key;
            public TValue value;

            public bool Equals(Entry other) {
                return this.key.Equals(other.key);
            }

            public override bool Equals(object obj) {
                return obj is Entry other && this.Equals(other);
            }

            public override int GetHashCode() {
                return this.key.GetHashCode();
            }

        }

        private IntrusiveHashSetGeneric<Entry> keys;

        public int Count => this.keys.Count;

        public TValue this[TKey key] {
            get {
                if (this.TryGetValue(in key, out var value) == true) {
                    return value;
                }
                return default;
            }
            set {
                this.Add(key, value);
            }
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() {

            return new Enumerator(this);

        }

        /// <summary>
        /// Put entity data into array.
        /// </summary>
        /// <returns>Buffer array from pool</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public BufferArray<Entry> ToArray() {

            var arr = PoolArray<Entry>.Spawn(this.Count);
            var i = 0;
            foreach (var entity in this) {
                
                arr.arr[i++] = entity;
                
            }

            return arr;
            
        }

        /// <summary>
        /// Clear the dictionary.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Clear() {

            this.keys.Clear();

        }

        /// <summary>
        /// Remove value by the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Returns TRUE if data was found</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool RemoveKey(in TKey key) {

            return this.keys.Remove(new Entry() { key = key });

        }

        /// <summary>
        /// Find an element by the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Returns TRUE if data was found</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(in TKey key) {

            return this.keys.Contains(new Entry() { key = key });
            
        }

        /// <summary>
        /// Add new value by the key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>Returns FALSE if key already exists</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Add(in TKey key, in TValue value) {

            var entry = new Entry() {
                key = key,
                value = value,
            };
            if (this.keys.Contains(entry) == false) {
                
                this.keys.Add(entry);
                return true;

            }

            return false;

        }

        /// <summary>
        /// Try to get value by the key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>Returns FALSE if key doesn't exist</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(in TKey key, out TValue value) {

            if (this.keys.Get(key.GetHashCode(), new Entry() { key = key }, out var result) == true) {

                value = result.value;
                return true;

            }

            value = default;
            return false;

        }

    }

}
