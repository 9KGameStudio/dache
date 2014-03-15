﻿using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Dache.Core.Performance;

namespace Dache.CacheHost.Storage
{
    /// <summary>
    /// Encapsulates a memory cache that can compress and store byte arrays. This type is thread safe.
    /// </summary>
    public class GZipMemCache : MemCache
    {
        public GZipMemCache(string cacheName, int physicalMemoryLimitPercentage, ICustomPerformanceCounterManager customPerformanceCounterManager)
            : base(cacheName, physicalMemoryLimitPercentage, customPerformanceCounterManager)
        { }

        /// <summary>
        /// Inserts or updates a byte array in the cache at the given key with the specified cache item policy.
        /// </summary>
        /// <param name="key">The key of the byte array. Null is not supported.</param>
        /// <param name="value">The byte array. Null is not supported.</param>
        /// <param name="cacheItemPolicy">The cache item policy.</param>
        /// <remarks>
        /// Passed byte array will be compressed by using <see cref="GZipStream"/>.
        /// </remarks>
        public override void Add(string key, byte[] value, CacheItemPolicy cacheItemPolicy)
        {
            // Sanitize
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("cannot be null, empty, or white space", "key");
            }
            if (value == null)
            {
                // GZipMemCache does not support null values
                throw new ArgumentNullException("value");
            }
            if (cacheItemPolicy == null)
            {
                throw new ArgumentNullException("cacheItemPolicy");
            }

            base.Add(key, Compress(value).Result, cacheItemPolicy);
        }

        /// <summary>
        /// Inserts or updates an interned byte array in the cache at the given key.
        /// Interned values cannot expire or be evicted unless removed manually.
        /// </summary>
        /// <param name="key">The key of the byte array. Null is not supported.</param>
        /// <param name="value">The byte array. Null is not supported.</param>
        /// <remarks>
        /// Passed byte array will be compressed by using <see cref="GZipStream"/>.
        /// </remarks>
        public override void AddInterned(string key, byte[] value)
        {
            // Sanitize
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("cannot be null, empty, or white space", "key");
            }
            if (value == null)
            {
                // GZipMemCache does not support null values
                throw new ArgumentNullException("value");
            }

            base.AddInterned(key, Compress(value).Result);
        }

        /// <summary>
        /// Gets a byte array from the cache.
        /// </summary>
        /// <param name="key">The key of the byte array.</param>
        /// <returns>
        /// The byte array if found, otherwise null.
        /// </returns>
        /// <remarks>
        /// Return byte array will be decompressed via <see cref="GZipStream"/> before being returned.
        /// </remarks>
        public override byte[] Get(string key)
        {
            // Sanitize
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return Decompress(base.Get(key)).Result;
        }

        /// <summary>
        /// Compresses the specified byte array.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Compressed byte array.</returns>
        /// <remarks>
        /// <see cref="GZipStream"/> is used for compression tasks.
        /// </remarks>
        protected async Task<byte[]> Compress(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Can't compress null value");
            }

            using (MemoryStream originalStream = new MemoryStream(value))
            {
                using (MemoryStream compressedStream = new MemoryStream())
                {
                    using (GZipStream compressionStream = new GZipStream(compressedStream, CompressionMode.Compress))
                    {
                        await originalStream.CopyToAsync(compressionStream);
                    }

                    return compressedStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Decompresses the specified byte array.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Decompressed byte array.</returns>
        /// <remarks>
        /// <see cref="GZipStream"/> is used for decompression tasks.
        /// </remarks>
        protected async Task<byte[]> Decompress(byte[] value)
        {
            // Sanitize
            if (value == null)
            {
                return null;
            }

            using (MemoryStream originalStream = new MemoryStream(value))
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    using (GZipStream decompressionStream = new GZipStream(originalStream, CompressionMode.Decompress))
                    {
                        await decompressionStream.CopyToAsync(decompressedStream);
                    }

                    return decompressedStream.ToArray();
                }
            }
        }
    }
}
