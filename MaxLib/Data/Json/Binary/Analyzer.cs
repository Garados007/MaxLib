using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Data.Json.Binary
{
    /// <summary>
    /// A <see cref="JsonElement"/> Analyzer that is required to calculate the enthropy of its data
    /// </summary>
    public class Analyzer
    {
        /// <summary>
        /// The test result how often an object key was used
        /// </summary>
        public Dictionary<string, long> ObjectKeys { get; private set; }

        /// <summary>
        /// the test result how often the chars in the object keys are used
        /// </summary>
        public Dictionary<char, long> ObjectKeyChars { get; private set; }

        /// <summary>
        /// the test result how often the chars in the object keys are used. The keys are 
        /// not counted twice. This measure will only work if <see cref="ObjectKeys"/>
        /// will count.
        /// </summary>
        public Dictionary<char, long> ObjectKeyCharUnique { get; private set; }

        /// <summary>
        /// the test result how often chars in string values are used globaly
        /// </summary>
        public Dictionary<char, long> GlobalStringChars { get; private set; }

        /// <summary>
        /// the test result how often chars in string values are used after the specific object keys
        /// </summary>
        public Dictionary<string, Dictionary<char, long>> ObjectStringChars { get; private set; }

        /// <summary>
        /// Create a new analyzer with the specified tests activated
        /// </summary>
        /// <param name="checkObjectKeys">will count how often object keys are used</param>
        /// <param name="checkObjectKeyChars">will count how often the chars in the object keys are used</param>
        /// <param name="checkObjectKeyCharUnique">
        /// will count how often the chars in the object keys are used. The keys
        /// are not counted twice. This test will only be activated if <paramref name="checkObjectKeys"/>
        /// is active.
        /// </param>
        /// <param name="checkGlobalStringChars">will count how often the chars in the string values are used globaly</param>
        /// <param name="checkObjectStringChars">will count how often the chars in string values are used under the object keys</param>
        public Analyzer(bool checkObjectKeys = true, bool checkObjectKeyChars = true, 
            bool checkObjectKeyCharUnique = true,
            bool checkGlobalStringChars = true, bool checkObjectStringChars = true)
        {
            if (checkObjectKeys)
                ObjectKeys = new Dictionary<string, long>();
            if (checkObjectKeyChars)
                ObjectKeyChars = new Dictionary<char, long>();
            if (checkObjectKeyCharUnique && checkObjectKeys)
                ObjectKeyCharUnique = new Dictionary<char, long>();
            if (checkGlobalStringChars)
                GlobalStringChars = new Dictionary<char, long>();
            if (checkObjectStringChars)
                ObjectStringChars = new Dictionary<string, Dictionary<char, long>>();
        }

        /// <summary>
        /// Will apply all defined test to the input
        /// </summary>
        /// <param name="element">the json element that should be analyzed</param>
        public virtual void Test(JsonElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            Test("", element);
        }

        protected virtual void Test(string parentKey, JsonElement element)
        {
            if (element.Object != null)
                Test(parentKey, element.Object);
            if (element.Array != null)
                Test(parentKey, element.Array);
            if (element.Value != null)
                Test(parentKey, element.Value);
        }

        protected virtual void Test(string parentKey, JsonObject element)
        {
            foreach (var item in element)
            {
                if (ObjectKeys != null)
                {
                    if (!ObjectKeys.TryGetValue(item.Key, out long used))
                    {
                        used = 0;
                        CountChars(item.Key, ObjectKeyCharUnique);
                    }
                    ObjectKeys[item.Key] = used + 1;
                }
                if (ObjectKeyChars != null)
                    CountChars(item.Key, ObjectKeyChars);
                Test(item.Key, item.Value);
            }
        }

        protected virtual void Test(string parentKey, JsonArray element)
        {
            foreach (var item in element)
                Test(parentKey, item);
        }

        protected virtual void Test(string parentKey, JsonValue element)
        {
            if (!element.ArgumentString.StartsWith("\""))
                return; //not a string
            var value = element.Get<string>();
            if (GlobalStringChars != null)
                CountChars(value, GlobalStringChars);
            if (ObjectStringChars != null)
            {
                if (!ObjectStringChars.TryGetValue(parentKey, out Dictionary<char, long> dict))
                    ObjectStringChars[parentKey] = dict = new Dictionary<char, long>();
                CountChars(value, dict);
            }
        }

        protected virtual void CountChars(string input, Dictionary<char, long> output)
        {
            if (output == null) return;
            foreach (var c in input)
            {
                if (!output.TryGetValue(c, out long used))
                    used = 0;
                output[c] = used + 1;
            }
        }
    }
}
