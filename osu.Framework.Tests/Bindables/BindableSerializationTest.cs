// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableSerializationTest
    {
        [Test]
        public void TestInt()
        {
            var toSerialize = new Bindable<int> { Value = 1337 };

            var deserialized = JsonConvert.DeserializeObject<Bindable<int>>(JsonConvert.SerializeObject(toSerialize));

            Assert.AreEqual(toSerialize.Value, deserialized.Value);
        }

        [Test]
        public void TestIntFromDerivedType()
        {
            var toSerialize = new BindableInt { Value = 1337 };

            var deserialized = JsonConvert.DeserializeObject<Bindable<int>>(JsonConvert.SerializeObject(toSerialize));

            Assert.AreEqual(toSerialize.Value, deserialized.Value);
        }

        [Test]
        public void TestDouble()
        {
            var toSerialize = new BindableDouble { Value = 1337.0 };

            var deserialized = JsonConvert.DeserializeObject<Bindable<double>>(JsonConvert.SerializeObject(toSerialize));

            Assert.AreEqual(toSerialize.Value, deserialized.Value);
        }

        [Test]
        public void TestString()
        {
            var toSerialize = new Bindable<string> { Value = "1337" };

            var deserialized = JsonConvert.DeserializeObject<Bindable<string>>(JsonConvert.SerializeObject(toSerialize));

            Assert.AreEqual(toSerialize.Value, deserialized.Value);
        }

        [Test]
        public void TestIntWithBounds()
        {
            var toSerialize = new CustomObj2
            {
                Bindable =
                {
                    MaxValue = int.MaxValue,
                    Value = 1337,
                }
            };

            var deserialized = JsonConvert.DeserializeObject<CustomObj2>(JsonConvert.SerializeObject(toSerialize));

            Assert.AreEqual(deserialized.Bindable.MaxValue, deserialized.Bindable.Value);
        }

        [Test]
        public void TestMultipleBindables()
        {
            var toSerialize = new CustomObj
            {
                Bindable1 = { Value = 1337 },
                Bindable2 = { Value = 1338 },
            };

            var deserialized = JsonConvert.DeserializeObject<CustomObj>(JsonConvert.SerializeObject(toSerialize));

            Assert.AreEqual(toSerialize.Bindable1.Value, deserialized.Bindable1.Value);
            Assert.AreEqual(toSerialize.Bindable2.Value, deserialized.Bindable2.Value);
        }

        [Test]
        public void TestComplexGeneric()
        {
            var toSerialize = new Bindable<CustomObj>
            {
                Value = new CustomObj
                {
                    Bindable1 = { Value = 1337 },
                    Bindable2 = { Value = 1338 },
                }
            };

            var deserialized = JsonConvert.DeserializeObject<Bindable<CustomObj>>(JsonConvert.SerializeObject(toSerialize));

            Assert.AreEqual(toSerialize.Value.Bindable1.Value, deserialized.Value.Bindable1.Value);
            Assert.AreEqual(toSerialize.Value.Bindable2.Value, deserialized.Value.Bindable2.Value);
        }

        private class CustomObj
        {
            public Bindable<int> Bindable1 = new Bindable<int>();
            public Bindable<int> Bindable2 = new Bindable<int>();
        }

        private class CustomObj2
        {
            public BindableInt Bindable = new BindableInt { MaxValue = 100 };
        }
    }
}
