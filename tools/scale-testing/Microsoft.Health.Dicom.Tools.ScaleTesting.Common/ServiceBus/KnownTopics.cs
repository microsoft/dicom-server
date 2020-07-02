// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.Common.ServiceBus
{
    public static class KnownTopics
    {
        private const string TestSuffix = "-test";

        public const string StowRs = "stow-rs";
        public const string StowRsTest = StowRs + TestSuffix;

        public const string WadoRs = "wado-rs";
        public const string WadoRsTest = WadoRs + TestSuffix;

        public const string WadoRsMetadata = "wado-rs-metadata";
        public const string WadoRsMetadataTest = WadoRsMetadata + TestSuffix;

        public const string Qido = "qido";
        public const string QidoTest = Qido + TestSuffix;
    }
}
