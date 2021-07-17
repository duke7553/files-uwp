using Files.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Files.Models.Settings
{
    /// <summary>
    /// Clipboard Canvas
    /// A base class to easily manage all application's settings.
    /// </summary>
    public abstract class BaseJsonSettingsModel
    {
        #region Protected Members

        protected readonly string settingsPath;

        protected readonly bool isCachingEnabled;

        protected Dictionary<string, object> settingsCache;

        #endregion Protected Members

        #region Constructor

        /// <inheritdoc cref="BaseJsonSettingsModel(string, bool)"/>
        public BaseJsonSettingsModel(string settingsPath)
            : this(settingsPath, false)
        {
        }

        /// <summary>
        /// Initializes new instance of <see cref="BaseJsonSettingsModel"/> and calls <see cref="Initialize"/> function.
        /// </summary>
        /// <param name="settingsPath">The path to settings file.</param>
        /// <param name="isCachingEnabled">Determines whether settings should be cached.
        /// Use is recommended when settings are accessed frequently to improve performance.
        /// <br/>
        /// <br/>
        /// If true, settings won't be flushed until value and <see cref="settingsCache"/> value is different.
        /// <br/>
        /// If false, settings are always accessed and flushed upon read, write.</param>
        public BaseJsonSettingsModel(string settingsPath, bool isCachingEnabled)
        {
            this.settingsPath = settingsPath;
            this.isCachingEnabled = isCachingEnabled;

            // Create new instance of the cache
            this.settingsCache = new Dictionary<string, object>();

            Initialize();
        }

        #endregion Constructor

        #region Helpers

        protected virtual async void Initialize()
        {
            // Create the file
            await ApplicationData.Current.LocalFolder.CreateFileAsync(settingsPath.Replace(ApplicationData.Current.LocalFolder.Path, string.Empty), CreationCollisionOption.OpenIfExists);
        }

        public virtual object ExportSettings()
        {
            return settingsCache;
        }

        public virtual void ImportSettings(object import)
        {
            try
            {
                // Try convert
                settingsCache = (Dictionary<string, object>)import;

                // Serialize
                string serialized = JsonConvert.SerializeObject(settingsCache, Formatting.Indented);

                // Write to file
                NativeFileOperationsHelper.WriteStringToFile(settingsPath, serialized);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debugger.Break();
            }
        }

        #endregion Helpers

        #region Get, Set

        protected virtual TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = "")
        {
            try
            {
                // Check if caching is enabled
                if (isCachingEnabled)
                {
                    // If the cache contains the setting...
                    if (settingsCache.ContainsKey(propertyName))
                    {
                        TValue settingValue;

                        // Get the object
                        object settingObject = settingsCache[propertyName];

                        // Check if it's a JToken object
                        if (settingObject is JToken jtoken)
                        {
                            // Get the value from JToken
                            settingValue = jtoken.ToObject<TValue>();
                        }
                        else
                        {
                            // Otherwise, it is TValue, get the value
                            settingValue = (TValue)settingObject;
                        }

                        // Return the setting and exit this function
                        return settingValue;
                    }

                    // Cache miss, the cache doesn't contain the setting, continue, to update the cache
                }

                // Read all settings from file
                string settingsData = NativeFileOperationsHelper.ReadStringFromFile(settingsPath);

                // If there are existing settings...
                if (!string.IsNullOrEmpty(settingsData))
                {
                    // Deserialize them and update the cache
                    settingsCache = JsonConvert.DeserializeObject<Dictionary<string, object>>(settingsData);
                }

                // If it doesn't have this setting...
                if (!settingsCache.ContainsKey(propertyName))
                {
                    // Add it to cache
                    settingsCache.Add(propertyName, defaultValue);

                    // Serialize with updated value
                    string serialized = JsonConvert.SerializeObject(settingsCache, Formatting.Indented);

                    // Write to file
                    NativeFileOperationsHelper.WriteStringToFile(settingsPath, serialized);
                }

                // Get the value object
                object valueObject = settingsCache[propertyName];
                if (valueObject is JToken jtoken2)
                {
                    return jtoken2.ToObject<TValue>();
                }

                return (TValue)valueObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debugger.Break();

                return default;
            }
        }

        protected virtual bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = "")
        {
            try
            {
                // If cache doesn't contain the setting...
                if (!settingsCache.ContainsKey(propertyName))
                {
                    // Add the setting
                    settingsCache.Add(propertyName, value);
                }
                else
                {
                    // Otherwise, update the setting's value
                    settingsCache[propertyName] = value;
                }

                // Serialize
                string serialized = JsonConvert.SerializeObject(settingsCache, Formatting.Indented);

                // Write to file
                NativeFileOperationsHelper.WriteStringToFile(settingsPath, serialized);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debugger.Break();

                return false;
            }
        }

        #endregion Get, Set
    }
}