"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[12],{

/***/ 85012:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ dicom_video_src)
});

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
;// CONCATENATED MODULE: ../../../extensions/dicom-video/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-dicom-video"}');
;// CONCATENATED MODULE: ../../../extensions/dicom-video/src/id.js

const id = package_namespaceObject.u2;
const SOPClassHandlerId = `${id}.sopClassHandlerModule.dicom-video`;

// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
;// CONCATENATED MODULE: ../../../extensions/dicom-video/src/getSopClassHandlerModule.js


const SOP_CLASS_UIDS = {
  VIDEO_MICROSCOPIC_IMAGE_STORAGE: '1.2.840.10008.5.1.4.1.1.77.1.2.1',
  VIDEO_PHOTOGRAPHIC_IMAGE_STORAGE: '1.2.840.10008.5.1.4.1.1.77.1.4.1',
  VIDEO_ENDOSCOPIC_IMAGE_STORAGE: '1.2.840.10008.5.1.4.1.1.77.1.1.1',
  /** Need to use fallback, could be video or image */
  SECONDARY_CAPTURE_IMAGE_STORAGE: '1.2.840.10008.5.1.4.1.1.7',
  MULTIFRAME_TRUE_COLOR_SECONDARY_CAPTURE_IMAGE_STORAGE: '1.2.840.10008.5.1.4.1.1.7.4'
};
const sopClassUids = Object.values(SOP_CLASS_UIDS);
const secondaryCaptureSopClassUids = [SOP_CLASS_UIDS.SECONDARY_CAPTURE_IMAGE_STORAGE, SOP_CLASS_UIDS.MULTIFRAME_TRUE_COLOR_SECONDARY_CAPTURE_IMAGE_STORAGE];
const SupportedTransferSyntaxes = {
  MPEG4_AVC_264_HIGH_PROFILE: '1.2.840.10008.1.2.4.102',
  MPEG4_AVC_264_BD_COMPATIBLE_HIGH_PROFILE: '1.2.840.10008.1.2.4.103',
  MPEG4_AVC_264_HIGH_PROFILE_FOR_2D_VIDEO: '1.2.840.10008.1.2.4.104',
  MPEG4_AVC_264_HIGH_PROFILE_FOR_3D_VIDEO: '1.2.840.10008.1.2.4.105',
  MPEG4_AVC_264_STEREO_HIGH_PROFILE: '1.2.840.10008.1.2.4.106',
  HEVC_265_MAIN_PROFILE: '1.2.840.10008.1.2.4.107',
  HEVC_265_MAIN_10_PROFILE: '1.2.840.10008.1.2.4.108'
};
const supportedTransferSyntaxUIDs = Object.values(SupportedTransferSyntaxes);
const _getDisplaySetsFromSeries = (instances, servicesManager, extensionManager) => {
  const dataSource = extensionManager.getActiveDataSource()[0];
  return instances.filter(metadata => {
    const tsuid = metadata.AvailableTransferSyntaxUID || metadata.TransferSyntaxUID || metadata['00083002'];
    if (supportedTransferSyntaxUIDs.includes(tsuid)) {
      return true;
    }
    if (metadata.SOPClassUID === SOP_CLASS_UIDS.VIDEO_PHOTOGRAPHIC_IMAGE_STORAGE) {
      return true;
    }

    // Assume that an instance with one of the secondary capture SOPClassUIDs and
    // with at least 90 frames (i.e. typically 3 seconds of video) is indeed a video.
    return secondaryCaptureSopClassUids.includes(metadata.SOPClassUID) && metadata.NumberOfFrames >= 90;
  }).map(instance => {
    const {
      Modality,
      SOPInstanceUID,
      SeriesDescription = 'VIDEO'
    } = instance;
    const {
      SeriesNumber,
      SeriesDate,
      SeriesInstanceUID,
      StudyInstanceUID,
      NumberOfFrames
    } = instance;
    const displaySet = {
      //plugin: id,
      Modality,
      displaySetInstanceUID: src.utils.guid(),
      SeriesDescription,
      SeriesNumber,
      SeriesDate,
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID,
      SOPClassHandlerId: SOPClassHandlerId,
      referencedImages: null,
      measurements: null,
      videoUrl: dataSource.retrieve.directURL({
        instance,
        singlepart: 'video',
        tag: 'PixelData'
      }),
      instances: [instance],
      thumbnailSrc: dataSource.retrieve.directURL({
        instance,
        defaultPath: '/thumbnail',
        defaultType: 'image/jpeg',
        tag: 'Absent'
      }),
      isDerivedDisplaySet: true,
      isLoaded: false,
      sopClassUids,
      numImageFrames: NumberOfFrames,
      instance
    };
    return displaySet;
  });
};
function getSopClassHandlerModule(_ref) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const getDisplaySetsFromSeries = instances => {
    return _getDisplaySetsFromSeries(instances, servicesManager, extensionManager);
  };
  return [{
    name: 'dicom-video',
    sopClassUids,
    getDisplaySetsFromSeries
  }];
}
;// CONCATENATED MODULE: ../../../extensions/dicom-video/src/index.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }



const Component = /*#__PURE__*/react.lazy(() => {
  return __webpack_require__.e(/* import() */ 686).then(__webpack_require__.bind(__webpack_require__, 39686));
});
const OHIFCornerstoneVideoViewport = props => {
  return /*#__PURE__*/react.createElement(react.Suspense, {
    fallback: /*#__PURE__*/react.createElement("div", null, "Loading...")
  }, /*#__PURE__*/react.createElement(Component, props));
};

/**
 *
 */
const dicomVideoExtension = {
  /**
   * Only required property. Should be a unique value across all extensions.
   */
  id: id,
  /**
   *
   *
   * @param {object} [configuration={}]
   * @param {object|array} [configuration.csToolsConfig] - Passed directly to `initCornerstoneTools`
   */
  getViewportModule(_ref) {
    let {
      servicesManager,
      extensionManager
    } = _ref;
    const ExtendedOHIFCornerstoneVideoViewport = props => {
      return /*#__PURE__*/react.createElement(OHIFCornerstoneVideoViewport, _extends({
        servicesManager: servicesManager,
        extensionManager: extensionManager
      }, props));
    };
    return [{
      name: 'dicom-video',
      component: ExtendedOHIFCornerstoneVideoViewport
    }];
  },
  // getCommandsModule({ servicesManager }) {
  //   return {
  //     definitions: {
  //       setToolActive: {
  //         commandFn: ({ toolName, element }) => {
  //         },
  //         storeContexts: [],
  //         options: {},
  //       },
  //     },
  //     defaultContext: 'ACTIVE_VIEWPORT::VIDEO',
  //   };
  // },
  getSopClassHandlerModule: getSopClassHandlerModule
};
function _getToolAlias(toolName) {
  let toolAlias = toolName;
  switch (toolName) {
    case 'EllipticalRoi':
      toolAlias = 'SREllipticalRoi';
      break;
  }
  return toolAlias;
}
/* harmony default export */ const dicom_video_src = (dicomVideoExtension);

/***/ })

}]);