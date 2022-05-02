// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions;

public class ConfigurationBinderExtensionsTests
{
    [Fact]
    public void GivenObject_WhenBindingToConfiguration_ThenWriteKeyValues()
    {
        var options = new ExampleOptions
        {
            DoubleNumber = 3.14D,
            FloatNumber = -10.11F,
            LongNumber = 98765L,
            Nested = new NestedOptions
            {
                Character = '!',
                DateAndTime = new DateTime(1000, 10, 10, 10, 10, 10, DateTimeKind.Utc),
                DecimalNumber = 0.9999M,
                OffsetDateAndTime = new DateTimeOffset(new DateTime(2000, 2, 2), TimeSpan.FromHours(3)),
                Text = "Hello World",
            },
            UIntNumber = 2468,
            ULongNumber = 1357911,
        };

        ExampleOptions.ByteArray = new byte[] { 1, 2, 3 };
        ExampleOptions.SByteNumber = 7;
        ExampleOptions.ShortEnumerable = new short[] { 4, 5, 6, 7, 8 };

        NestedOptions.AnotherLevel = new ReallyNestedOptions
        {
            Duration = TimeSpan.Parse("11.22:33:44"),
            Id = Guid.Parse("a5980f6d-abe4-4495-a972-8b9844962d28"),
            IntList = new List<int> { 10, 20, -30, -40 },
            Resource = new Uri("https://www.bing.com"),
        };

        NestedOptions.UShortCollection = new ushort[] { 1 };

        IConfigurationSection section;
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config.Set(options);

        Assert.Equal("7", config[nameof(ExampleOptions.SByteNumber)]);
        Assert.Equal("2468", config[nameof(ExampleOptions.UIntNumber)]);
        Assert.Equal("98765", config[nameof(ExampleOptions.LongNumber)]);
        Assert.Equal("1357911", config[nameof(ExampleOptions.ULongNumber)]);
        Assert.Equal("-10.11", config[nameof(ExampleOptions.FloatNumber)]);
        Assert.Equal("3.14", config[nameof(ExampleOptions.DoubleNumber)]);

        section = config.GetSection(nameof(ExampleOptions.ByteArray));
        Assert.Equal(3, section.GetChildren().Count(x => x.Value != null));
        Assert.Equal("1", section["0"]);
        Assert.Equal("2", section["1"]);
        Assert.Equal("3", section["2"]);

        section = config.GetSection(nameof(ExampleOptions.ShortEnumerable));
        Assert.Equal(5, section.GetChildren().Count(x => x.Value != null));
        Assert.Equal("4", section["0"]);
        Assert.Equal("5", section["1"]);
        Assert.Equal("6", section["2"]);
        Assert.Equal("7", section["3"]);
        Assert.Equal("8", section["4"]);

        config = config.GetSection(nameof(ExampleOptions.Nested));
        Assert.Equal("0.9999", config[nameof(NestedOptions.DecimalNumber)]);
        Assert.Equal("!", config[nameof(NestedOptions.Character)]);
        Assert.Equal("Hello World", config[nameof(NestedOptions.Text)]);
        Assert.Equal("1000-10-10T10:10:10.0000000Z", config[nameof(NestedOptions.DateAndTime)]);
        Assert.Equal("2000-02-02T00:00:00.0000000+03:00", config[nameof(NestedOptions.OffsetDateAndTime)]);

        section = config.GetSection(nameof(NestedOptions.UShortCollection));
        Assert.Equal(1, section.GetChildren().Count(x => x.Value != null));
        Assert.Equal("1", section["0"]);

        config = config.GetSection(nameof(NestedOptions.AnotherLevel));
        Assert.Equal("11.22:33:44", config[nameof(ReallyNestedOptions.Duration)]);
        Assert.Equal("a5980f6d-abe4-4495-a972-8b9844962d28", config[nameof(ReallyNestedOptions.Id)]);
        Assert.Equal("https://www.bing.com", config[nameof(ReallyNestedOptions.Resource)]);

        section = config.GetSection(nameof(ReallyNestedOptions.IntList));
        Assert.Equal(4, section.GetChildren().Count(x => x.Value != null));
        Assert.Equal("10", section["0"]);
        Assert.Equal("20", section["1"]);
        Assert.Equal("-30", section["2"]);
        Assert.Equal("-40", section["3"]);
    }

    private sealed class ExampleOptions
    {
        public static sbyte SByteNumber { get; set; }

        public static byte[] ByteArray { get; set; }

        public static IEnumerable<short> ShortEnumerable { get; set; }

        public uint UIntNumber { get; set; }

        public long LongNumber { get; set; }

        public ulong ULongNumber { get; set; }

        public float FloatNumber { get; set; }

        public double DoubleNumber { get; set; }

        public NestedOptions Nested { get; set; }
    }

    private sealed class NestedOptions
    {
        public static IReadOnlyCollection<ushort> UShortCollection { get; set; }

        public static ReallyNestedOptions AnotherLevel { get; set; }

        public decimal DecimalNumber { get; set; }

        public char Character { get; set; }

        public string Text { get; set; }

        public DateTime DateAndTime { get; set; }

        public DateTimeOffset OffsetDateAndTime { get; set; }
    }

    private sealed class ReallyNestedOptions
    {
        public IReadOnlyList<int> IntList { get; set; }

        public TimeSpan Duration { get; set; }

        public Guid Id { get; set; }

        public Uri Resource { get; set; }
    }
}
