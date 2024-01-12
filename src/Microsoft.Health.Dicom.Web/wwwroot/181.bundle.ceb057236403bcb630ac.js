"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[181],{

/***/ 86181:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ Viewport_OHIFCornerstoneViewport)
});

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../../node_modules/react-resize-detector/build/index.esm.js
var index_esm = __webpack_require__(7023);
// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/tools/dist/esm/index.js + 348 modules
var esm = __webpack_require__(14957);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var dist_esm = __webpack_require__(3743);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
// EXTERNAL MODULE: ../../../extensions/cornerstone/src/state.ts
var state = __webpack_require__(73704);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/OHIFCornerstoneViewport.css
// extracted by mini-css-extract-plugin

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/Overlays/ViewportImageScrollbar.tsx





function CornerstoneImageScrollbar(_ref) {
  let {
    viewportData,
    viewportId,
    element,
    imageSliceData,
    setImageSliceData,
    scrollbarHeight,
    servicesManager
  } = _ref;
  const {
    cineService,
    cornerstoneViewportService
  } = servicesManager.services;
  const onImageScrollbarChange = (imageIndex, viewportId) => {
    const viewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
    const {
      isCineEnabled
    } = cineService.getState();
    if (isCineEnabled) {
      // on image scrollbar change, stop the CINE if it is playing
      cineService.stopClip(element);
      cineService.setCine({
        id: viewportId,
        isPlaying: false
      });
    }
    esm.utilities.jumpToSlice(viewport.element, {
      imageIndex,
      debounceLoading: true
    });
  };
  (0,react.useEffect)(() => {
    if (!viewportData) {
      return;
    }
    const viewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
    if (!viewport) {
      return;
    }
    if (viewportData.viewportType === dist_esm.Enums.ViewportType.STACK) {
      const imageIndex = viewport.getCurrentImageIdIndex();
      setImageSliceData({
        imageIndex: imageIndex,
        numberOfSlices: viewportData.data.imageIds.length
      });
      return;
    }
    if (viewportData.viewportType === dist_esm.Enums.ViewportType.ORTHOGRAPHIC) {
      const sliceData = dist_esm.utilities.getImageSliceDataForVolumeViewport(viewport);
      if (!sliceData) {
        return;
      }
      const {
        imageIndex,
        numberOfSlices
      } = sliceData;
      setImageSliceData({
        imageIndex,
        numberOfSlices
      });
    }
  }, [viewportId, viewportData]);
  (0,react.useEffect)(() => {
    if (viewportData?.viewportType !== dist_esm.Enums.ViewportType.STACK) {
      return;
    }
    const updateStackIndex = event => {
      const {
        newImageIdIndex
      } = event.detail;
      // find the index of imageId in the imageIds
      setImageSliceData({
        imageIndex: newImageIdIndex,
        numberOfSlices: viewportData.data.imageIds.length
      });
    };
    element.addEventListener(dist_esm.Enums.Events.STACK_VIEWPORT_SCROLL, updateStackIndex);
    return () => {
      element.removeEventListener(dist_esm.Enums.Events.STACK_VIEWPORT_SCROLL, updateStackIndex);
    };
  }, [viewportData, element]);
  (0,react.useEffect)(() => {
    if (viewportData?.viewportType !== dist_esm.Enums.ViewportType.ORTHOGRAPHIC) {
      return;
    }
    const updateVolumeIndex = event => {
      const {
        imageIndex,
        numberOfSlices
      } = event.detail;
      // find the index of imageId in the imageIds
      setImageSliceData({
        imageIndex,
        numberOfSlices
      });
    };
    element.addEventListener(dist_esm.Enums.Events.VOLUME_NEW_IMAGE, updateVolumeIndex);
    return () => {
      element.removeEventListener(dist_esm.Enums.Events.VOLUME_NEW_IMAGE, updateVolumeIndex);
    };
  }, [viewportData, element]);
  return /*#__PURE__*/react.createElement(ui_src/* ImageScrollbar */.Ln, {
    onChange: evt => onImageScrollbarChange(evt, viewportId),
    max: imageSliceData.numberOfSlices ? imageSliceData.numberOfSlices - 1 : 0,
    height: scrollbarHeight,
    value: imageSliceData.imageIndex
  });
}
CornerstoneImageScrollbar.propTypes = {
  viewportData: (prop_types_default()).object,
  viewportId: (prop_types_default()).string.isRequired,
  element: prop_types_default().instanceOf(Element),
  scrollbarHeight: (prop_types_default()).string,
  imageSliceData: (prop_types_default()).object.isRequired,
  setImageSliceData: (prop_types_default()).func.isRequired,
  servicesManager: (prop_types_default()).object.isRequired
};
/* harmony default export */ const ViewportImageScrollbar = (CornerstoneImageScrollbar);
// EXTERNAL MODULE: ../../../node_modules/gl-matrix/esm/index.js + 10 modules
var gl_matrix_esm = __webpack_require__(45451);
// EXTERNAL MODULE: ../../../node_modules/moment/moment.js
var moment = __webpack_require__(71271);
var moment_default = /*#__PURE__*/__webpack_require__.n(moment);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/Overlays/utils.ts



/**
 * Checks if value is valid.
 *
 * @param {number} value
 * @returns {boolean} is valid.
 */
function isValidNumber(value) {
  return typeof value === 'number' && !isNaN(value);
}

/**
 * Formats number precision.
 *
 * @param {number} number
 * @param {number} precision
 * @returns {number} formatted number.
 */
function formatNumberPrecision(number) {
  let precision = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 0;
  if (number !== null) {
    return parseFloat(number).toFixed(precision);
  }
}

/**
 * Formats DICOM date.
 *
 * @param {string} date
 * @param {string} strFormat
 * @returns {string} formatted date.
 */
function formatDICOMDate(date) {
  let strFormat = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 'MMM D, YYYY';
  return moment_default()(date, 'YYYYMMDD').format(strFormat);
}

/**
 *    DICOM Time is stored as HHmmss.SSS, where:
 *      HH 24 hour time:
 *        m mm        0..59   Minutes
 *        s ss        0..59   Seconds
 *        S SS SSS    0..999  Fractional seconds
 *
 *        Goal: '24:12:12'
 *
 * @param {*} time
 * @param {string} strFormat
 * @returns {string} formatted name.
 */
function formatDICOMTime(time) {
  let strFormat = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 'HH:mm:ss';
  return moment_default()(time, 'HH:mm:ss').format(strFormat);
}

/**
 * Formats a patient name for display purposes
 *
 * @param {string} name
 * @returns {string} formatted name.
 */
function formatPN(name) {
  if (!name) {
    return '';
  }
  const cleaned = name.split('^').filter(s => !!s).join(', ').trim();
  return cleaned === ',' || cleaned === '' ? '' : cleaned;
}

/**
 * Gets compression type
 *
 * @param {number} imageId
 * @returns {string} compression type.
 */
function getCompression(imageId) {
  const generalImageModule = metaData.get('generalImageModule', imageId) || {};
  const {
    lossyImageCompression,
    lossyImageCompressionRatio,
    lossyImageCompressionMethod
  } = generalImageModule;
  if (lossyImageCompression === '01' && lossyImageCompressionRatio !== '') {
    const compressionMethod = lossyImageCompressionMethod || 'Lossy: ';
    const compressionRatio = formatNumberPrecision(lossyImageCompressionRatio, 2);
    return compressionMethod + compressionRatio + ' : 1';
  }
  return 'Lossless / Uncompressed';
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/Overlays/CustomizableViewportOverlay.css
// extracted by mini-css-extract-plugin

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/Overlays/CustomizableViewportOverlay.tsx







const EPSILON = 1e-4;
/**
 * Window Level / Center Overlay item
 */
function VOIOverlayItem(_ref) {
  let {
    voi,
    customization
  } = _ref;
  const {
    windowWidth,
    windowCenter
  } = voi;
  if (typeof windowCenter !== 'number' || typeof windowWidth !== 'number') {
    return null;
  }
  return /*#__PURE__*/react.createElement("div", {
    className: "overlay-item flex flex-row",
    style: {
      color: customization && customization.color || undefined
    }
  }, /*#__PURE__*/react.createElement("span", {
    className: "mr-1 shrink-0"
  }, "W:"), /*#__PURE__*/react.createElement("span", {
    className: "ml-1 mr-2 shrink-0 font-light"
  }, windowWidth.toFixed(0)), /*#__PURE__*/react.createElement("span", {
    className: "mr-1 shrink-0"
  }, "L:"), /*#__PURE__*/react.createElement("span", {
    className: "ml-1 shrink-0 font-light"
  }, windowCenter.toFixed(0)));
}

/**
 * Zoom Level Overlay item
 */
function ZoomOverlayItem(_ref2) {
  let {
    scale,
    customization
  } = _ref2;
  return /*#__PURE__*/react.createElement("div", {
    className: "overlay-item flex flex-row",
    style: {
      color: customization && customization.color || undefined
    }
  }, /*#__PURE__*/react.createElement("span", {
    className: "mr-1 shrink-0"
  }, "Zoom:"), /*#__PURE__*/react.createElement("span", {
    className: "font-light"
  }, scale.toFixed(2), "x"));
}

/**
 * Instance Number Overlay Item
 */
function InstanceNumberOverlayItem(_ref3) {
  let {
    instanceNumber,
    imageSliceData,
    customization
  } = _ref3;
  const {
    imageIndex,
    numberOfSlices
  } = imageSliceData;
  return /*#__PURE__*/react.createElement("div", {
    className: "overlay-item flex flex-row",
    style: {
      color: customization && customization.color || undefined
    }
  }, /*#__PURE__*/react.createElement("span", {
    className: "mr-1 shrink-0"
  }, "I:"), /*#__PURE__*/react.createElement("span", {
    className: "font-light"
  }, instanceNumber !== undefined && instanceNumber !== null ? `${instanceNumber} (${imageIndex + 1}/${numberOfSlices})` : `${imageIndex + 1}/${numberOfSlices}`));
}

/**
 * Customizable Viewport Overlay
 */
function CustomizableViewportOverlay(_ref4) {
  let {
    element,
    viewportData,
    imageSliceData,
    viewportId,
    servicesManager
  } = _ref4;
  const {
    toolbarService,
    cornerstoneViewportService,
    customizationService
  } = servicesManager.services;
  const [voi, setVOI] = (0,react.useState)({
    windowCenter: null,
    windowWidth: null
  });
  const [scale, setScale] = (0,react.useState)(1);
  const [activeTools, setActiveTools] = (0,react.useState)([]);
  const {
    imageIndex
  } = imageSliceData;
  const topLeftCustomization = customizationService.getModeCustomization('cornerstoneOverlayTopLeft');
  const topRightCustomization = customizationService.getModeCustomization('cornerstoneOverlayTopRight');
  const bottomLeftCustomization = customizationService.getModeCustomization('cornerstoneOverlayBottomLeft');
  const bottomRightCustomization = customizationService.getModeCustomization('cornerstoneOverlayBottomRight');
  const instance = (0,react.useMemo)(() => {
    if (viewportData != null) {
      return _getViewportInstance(viewportData, imageIndex);
    } else {
      return null;
    }
  }, [viewportData, imageIndex]);
  const instanceNumber = (0,react.useMemo)(() => {
    if (viewportData != null) {
      return _getInstanceNumber(viewportData, viewportId, imageIndex, cornerstoneViewportService);
    }
    return null;
  }, [viewportData, viewportId, imageIndex, cornerstoneViewportService]);

  /**
   * Initial toolbar state
   */
  (0,react.useEffect)(() => {
    setActiveTools(toolbarService.getActiveTools());
  }, []);

  /**
   * Updating the VOI when the viewport changes its voi
   */
  (0,react.useEffect)(() => {
    const updateVOI = eventDetail => {
      const {
        range
      } = eventDetail.detail;
      if (!range) {
        return;
      }
      const {
        lower,
        upper
      } = range;
      const {
        windowWidth,
        windowCenter
      } = dist_esm.utilities.windowLevel.toWindowLevel(lower, upper);
      setVOI({
        windowCenter,
        windowWidth
      });
    };
    element.addEventListener(dist_esm.Enums.Events.VOI_MODIFIED, updateVOI);
    return () => {
      element.removeEventListener(dist_esm.Enums.Events.VOI_MODIFIED, updateVOI);
    };
  }, [viewportId, viewportData, voi, element]);

  /**
   * Updating the scale when the viewport changes its zoom
   */
  (0,react.useEffect)(() => {
    const updateScale = eventDetail => {
      const {
        previousCamera,
        camera
      } = eventDetail.detail;
      if (previousCamera.parallelScale !== camera.parallelScale || previousCamera.scale !== camera.scale) {
        const viewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
        if (!viewport) {
          return;
        }
        const imageData = viewport.getImageData();
        if (!imageData) {
          return;
        }
        if (camera.scale) {
          setScale(camera.scale);
          return;
        }
        const {
          spacing
        } = imageData;
        // convert parallel scale to scale
        const scale = element.clientHeight * spacing[0] * 0.5 / camera.parallelScale;
        setScale(scale);
      }
    };
    element.addEventListener(dist_esm.Enums.Events.CAMERA_MODIFIED, updateScale);
    return () => {
      element.removeEventListener(dist_esm.Enums.Events.CAMERA_MODIFIED, updateScale);
    };
  }, [viewportId, viewportData, cornerstoneViewportService, element]);

  /**
   * Updating the active tools when the toolbar changes
   */
  // Todo: this should act on the toolGroups instead of the toolbar state
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = toolbarService.subscribe(toolbarService.EVENTS.TOOL_BAR_STATE_MODIFIED, () => {
      setActiveTools(toolbarService.getActiveTools());
    });
    return () => {
      unsubscribe();
    };
  }, [toolbarService]);
  const _renderOverlayItem = (0,react.useCallback)(item => {
    const overlayItemProps = {
      element,
      viewportData,
      imageSliceData,
      viewportId,
      servicesManager,
      customization: item,
      formatters: {
        formatPN: formatPN,
        formatDate: formatDICOMDate,
        formatTime: formatDICOMTime,
        formatNumberPrecision: formatNumberPrecision
      },
      instance,
      // calculated
      voi,
      scale,
      instanceNumber
    };
    if (item.customizationType === 'ohif.overlayItem.windowLevel') {
      return /*#__PURE__*/react.createElement(VOIOverlayItem, overlayItemProps);
    } else if (item.customizationType === 'ohif.overlayItem.zoomLevel') {
      return /*#__PURE__*/react.createElement(ZoomOverlayItem, overlayItemProps);
    } else if (item.customizationType === 'ohif.overlayItem.instanceNumber') {
      return /*#__PURE__*/react.createElement(InstanceNumberOverlayItem, overlayItemProps);
    } else {
      const renderItem = customizationService.transform(item);
      if (typeof renderItem.content === 'function') {
        return renderItem.content(overlayItemProps);
      }
    }
  }, [element, viewportData, imageSliceData, viewportId, servicesManager, customizationService, instance, voi, scale, instanceNumber]);
  const getTopLeftContent = (0,react.useCallback)(() => {
    const items = topLeftCustomization?.items || [{
      id: 'WindowLevel',
      customizationType: 'ohif.overlayItem.windowLevel'
    }];
    return /*#__PURE__*/react.createElement(react.Fragment, null, items.map((item, i) => /*#__PURE__*/react.createElement("div", {
      key: `topLeftOverlayItem_${i}`
    }, _renderOverlayItem(item))));
  }, [topLeftCustomization, _renderOverlayItem]);
  const getTopRightContent = (0,react.useCallback)(() => {
    const items = topRightCustomization?.items || [{
      id: 'InstanceNmber',
      customizationType: 'ohif.overlayItem.instanceNumber'
    }];
    return /*#__PURE__*/react.createElement(react.Fragment, null, items.map((item, i) => /*#__PURE__*/react.createElement("div", {
      key: `topRightOverlayItem_${i}`
    }, _renderOverlayItem(item))));
  }, [topRightCustomization, _renderOverlayItem]);
  const getBottomLeftContent = (0,react.useCallback)(() => {
    const items = bottomLeftCustomization?.items || [];
    return /*#__PURE__*/react.createElement(react.Fragment, null, items.map((item, i) => /*#__PURE__*/react.createElement("div", {
      key: `bottomLeftOverlayItem_${i}`
    }, _renderOverlayItem(item))));
  }, [bottomLeftCustomization, _renderOverlayItem]);
  const getBottomRightContent = (0,react.useCallback)(() => {
    const items = bottomRightCustomization?.items || [];
    return /*#__PURE__*/react.createElement(react.Fragment, null, items.map((item, i) => /*#__PURE__*/react.createElement("div", {
      key: `bottomRightOverlayItem_${i}`
    }, _renderOverlayItem(item))));
  }, [bottomRightCustomization, _renderOverlayItem]);
  return /*#__PURE__*/react.createElement(ui_src/* ViewportOverlay */.No, {
    topLeft: getTopLeftContent(),
    topRight: getTopRightContent(),
    bottomLeft: getBottomLeftContent(),
    bottomRight: getBottomRightContent()
  });
}
function _getViewportInstance(viewportData, imageIndex) {
  let imageId = null;
  if (viewportData.viewportType === dist_esm.Enums.ViewportType.STACK) {
    imageId = viewportData.data.imageIds[imageIndex];
  } else if (viewportData.viewportType === dist_esm.Enums.ViewportType.ORTHOGRAPHIC) {
    const volumes = viewportData.data;
    if (volumes && volumes.length == 1) {
      const volume = volumes[0];
      imageId = volume.imageIds[imageIndex];
    }
  }
  return imageId ? dist_esm.metaData.get('instance', imageId) || {} : {};
}
function _getInstanceNumber(viewportData, viewportId, imageIndex, cornerstoneViewportService) {
  let instanceNumber;
  if (viewportData.viewportType === dist_esm.Enums.ViewportType.STACK) {
    instanceNumber = _getInstanceNumberFromStack(viewportData, imageIndex);
    if (!instanceNumber && instanceNumber !== 0) {
      return null;
    }
  } else if (viewportData.viewportType === dist_esm.Enums.ViewportType.ORTHOGRAPHIC) {
    instanceNumber = _getInstanceNumberFromVolume(viewportData, imageIndex, viewportId, cornerstoneViewportService);
  }
  return instanceNumber;
}
function _getInstanceNumberFromStack(viewportData, imageIndex) {
  const imageIds = viewportData.data.imageIds;
  const imageId = imageIds[imageIndex];
  if (!imageId) {
    return;
  }
  const generalImageModule = dist_esm.metaData.get('generalImageModule', imageId) || {};
  const {
    instanceNumber
  } = generalImageModule;
  const stackSize = imageIds.length;
  if (stackSize <= 1) {
    return;
  }
  return parseInt(instanceNumber);
}

// Since volume viewports can be in any view direction, they can render
// a reconstructed image which don't have imageIds; therefore, no instance and instanceNumber
// Here we check if viewport is in the acquisition direction and if so, we get the instanceNumber
function _getInstanceNumberFromVolume(viewportData, viewportId, cornerstoneViewportService) {
  const volumes = viewportData.volumes;

  // Todo: support fusion of acquisition plane which has instanceNumber
  if (!volumes || volumes.length > 1) {
    return;
  }
  const volume = volumes[0];
  const {
    direction,
    imageIds
  } = volume;
  const cornerstoneViewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
  if (!cornerstoneViewport) {
    return;
  }
  const camera = cornerstoneViewport.getCamera();
  const {
    viewPlaneNormal
  } = camera;
  // checking if camera is looking at the acquisition plane (defined by the direction on the volume)

  const scanAxisNormal = direction.slice(6, 9);

  // check if viewPlaneNormal is parallel to scanAxisNormal
  const cross = gl_matrix_esm/* vec3.cross */.R3.cross(gl_matrix_esm/* vec3.create */.R3.create(), viewPlaneNormal, scanAxisNormal);
  const isAcquisitionPlane = gl_matrix_esm/* vec3.length */.R3.length(cross) < EPSILON;
  if (isAcquisitionPlane) {
    const imageId = imageIds[imageIndex];
    if (!imageId) {
      return {};
    }
    const {
      instanceNumber
    } = dist_esm.metaData.get('generalImageModule', imageId) || {};
    return parseInt(instanceNumber);
  }
}
CustomizableViewportOverlay.propTypes = {
  viewportData: (prop_types_default()).object,
  imageIndex: (prop_types_default()).number,
  viewportId: (prop_types_default()).string
};
/* harmony default export */ const Overlays_CustomizableViewportOverlay = (CustomizableViewportOverlay);
// EXTERNAL MODULE: ../../../node_modules/classnames/index.js
var classnames = __webpack_require__(44921);
var classnames_default = /*#__PURE__*/__webpack_require__.n(classnames);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/Overlays/ViewportOrientationMarkers.css
// extracted by mini-css-extract-plugin

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/Overlays/ViewportOrientationMarkers.tsx







const {
  getOrientationStringLPS,
  invertOrientationStringLPS
} = esm.utilities.orientation;
function ViewportOrientationMarkers(_ref) {
  let {
    element,
    viewportData,
    imageSliceData,
    viewportId,
    servicesManager,
    orientationMarkers = ['top', 'left']
  } = _ref;
  // Rotation is in degrees
  const [rotation, setRotation] = (0,react.useState)(0);
  const [flipHorizontal, setFlipHorizontal] = (0,react.useState)(false);
  const [flipVertical, setFlipVertical] = (0,react.useState)(false);
  const {
    cornerstoneViewportService
  } = servicesManager.services;
  (0,react.useEffect)(() => {
    const cameraModifiedListener = evt => {
      const {
        rotation,
        previousCamera,
        camera
      } = evt.detail;
      if (rotation !== undefined) {
        setRotation(rotation);
      }
      if (camera.flipHorizontal !== undefined && previousCamera.flipHorizontal !== camera.flipHorizontal) {
        setFlipHorizontal(camera.flipHorizontal);
      }
      if (camera.flipVertical !== undefined && previousCamera.flipVertical !== camera.flipVertical) {
        setFlipVertical(camera.flipVertical);
      }
    };
    element.addEventListener(dist_esm.Enums.Events.CAMERA_MODIFIED, cameraModifiedListener);
    return () => {
      element.removeEventListener(dist_esm.Enums.Events.CAMERA_MODIFIED, cameraModifiedListener);
    };
  }, []);
  const markers = (0,react.useMemo)(() => {
    if (!viewportData) {
      return '';
    }
    let rowCosines, columnCosines;
    if (viewportData.viewportType === 'stack') {
      const imageIndex = imageSliceData.imageIndex;
      const imageId = viewportData.data.imageIds?.[imageIndex];

      // Workaround for below TODO stub
      if (!imageId) {
        return false;
      }
      ({
        rowCosines,
        columnCosines
      } = dist_esm.metaData.get('imagePlaneModule', imageId) || {});
    } else {
      if (!element || !(0,dist_esm.getEnabledElement)(element)) {
        return '';
      }
      const {
        viewport
      } = (0,dist_esm.getEnabledElement)(element);
      const {
        viewUp,
        viewPlaneNormal
      } = viewport.getCamera();
      const viewRight = gl_matrix_esm/* vec3.create */.R3.create();
      gl_matrix_esm/* vec3.cross */.R3.cross(viewRight, viewUp, viewPlaneNormal);
      columnCosines = [-viewUp[0], -viewUp[1], -viewUp[2]];
      rowCosines = viewRight;
    }
    if (!rowCosines || !columnCosines || rotation === undefined) {
      return '';
    }
    const markers = _getOrientationMarkers(rowCosines, columnCosines, rotation, flipVertical, flipHorizontal);
    const ohifViewport = cornerstoneViewportService.getViewportInfo(viewportId);
    if (!ohifViewport) {
      console.log('ViewportOrientationMarkers::No viewport');
      return null;
    }
    const backgroundColor = ohifViewport.getViewportOptions().background;

    // Todo: probably this can be done in a better way in which we identify bright
    // background
    const isLight = backgroundColor ? dist_esm.utilities.isEqual(backgroundColor, [1, 1, 1]) : false;
    return orientationMarkers.map((m, index) => /*#__PURE__*/react.createElement("div", {
      className: classnames_default()(`${m}-mid orientation-marker`, isLight ? 'text-[#726F7E]' : 'text-[#ccc]'),
      key: `${m}-mid orientation-marker`
    }, /*#__PURE__*/react.createElement("div", {
      className: "orientation-marker-value"
    }, markers[m])));
  }, [viewportData, imageSliceData, rotation, flipVertical, flipHorizontal, orientationMarkers, element]);
  return /*#__PURE__*/react.createElement("div", {
    className: "ViewportOrientationMarkers noselect"
  }, markers);
}
ViewportOrientationMarkers.propTypes = {
  percentComplete: (prop_types_default()).number,
  error: (prop_types_default()).object
};
ViewportOrientationMarkers.defaultProps = {
  percentComplete: 0,
  error: null
};

/**
 *
 * Computes the orientation labels on a Cornerstone-enabled Viewport element
 * when the viewport settings change (e.g. when a horizontal flip or a rotation occurs)
 *
 * @param {*} rowCosines
 * @param {*} columnCosines
 * @param {*} rotation in degrees
 * @returns
 */
function _getOrientationMarkers(rowCosines, columnCosines, rotation, flipVertical, flipHorizontal) {
  const rowString = getOrientationStringLPS(rowCosines);
  const columnString = getOrientationStringLPS(columnCosines);
  const oppositeRowString = invertOrientationStringLPS(rowString);
  const oppositeColumnString = invertOrientationStringLPS(columnString);
  const markers = {
    top: oppositeColumnString,
    left: oppositeRowString,
    right: rowString,
    bottom: columnString
  };

  // If any vertical or horizontal flips are applied, change the orientation strings ahead of
  // the rotation applications
  if (flipVertical) {
    markers.top = invertOrientationStringLPS(markers.top);
    markers.bottom = invertOrientationStringLPS(markers.bottom);
  }
  if (flipHorizontal) {
    markers.left = invertOrientationStringLPS(markers.left);
    markers.right = invertOrientationStringLPS(markers.right);
  }

  // Swap the labels accordingly if the viewport has been rotated
  // This could be done in a more complex way for intermediate rotation values (e.g. 45 degrees)
  if (rotation === 90 || rotation === -270) {
    return {
      top: markers.left,
      left: invertOrientationStringLPS(markers.top),
      right: invertOrientationStringLPS(markers.bottom),
      bottom: markers.right // left
    };
  } else if (rotation === -90 || rotation === 270) {
    return {
      top: invertOrientationStringLPS(markers.left),
      left: markers.top,
      bottom: markers.left,
      right: markers.bottom
    };
  } else if (rotation === 180 || rotation === -180) {
    return {
      top: invertOrientationStringLPS(markers.top),
      left: invertOrientationStringLPS(markers.left),
      bottom: invertOrientationStringLPS(markers.bottom),
      right: invertOrientationStringLPS(markers.right)
    };
  }
  return markers;
}
/* harmony default export */ const Overlays_ViewportOrientationMarkers = (ViewportOrientationMarkers);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/Overlays/ViewportImageSliceLoadingIndicator.tsx



function ViewportImageSliceLoadingIndicator(_ref) {
  let {
    viewportData,
    element
  } = _ref;
  const [loading, setLoading] = (0,react.useState)(false);
  const [error, setError] = (0,react.useState)(false);
  const loadIndicatorRef = (0,react.useRef)(null);
  const imageIdToBeLoaded = (0,react.useRef)(null);
  const setLoadingState = evt => {
    clearTimeout(loadIndicatorRef.current);
    loadIndicatorRef.current = setTimeout(() => {
      setLoading(true);
    }, 50);
  };
  const setFinishLoadingState = evt => {
    clearTimeout(loadIndicatorRef.current);
    setLoading(false);
  };
  const setErrorState = evt => {
    clearTimeout(loadIndicatorRef.current);
    if (imageIdToBeLoaded.current === evt.detail.imageId) {
      setError(evt.detail.error);
      imageIdToBeLoaded.current = null;
    }
  };
  (0,react.useEffect)(() => {
    element.addEventListener(dist_esm.Enums.Events.STACK_VIEWPORT_SCROLL, setLoadingState);
    element.addEventListener(dist_esm.Enums.Events.IMAGE_LOAD_ERROR, setErrorState);
    element.addEventListener(dist_esm.Enums.Events.STACK_NEW_IMAGE, setFinishLoadingState);
    return () => {
      element.removeEventListener(dist_esm.Enums.Events.STACK_VIEWPORT_SCROLL, setLoadingState);
      element.removeEventListener(dist_esm.Enums.Events.STACK_NEW_IMAGE, setFinishLoadingState);
      element.removeEventListener(dist_esm.Enums.Events.IMAGE_LOAD_ERROR, setErrorState);
    };
  }, [element, viewportData]);
  if (error) {
    return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("div", {
      className: "absolute top-0 left-0 h-full w-full bg-black opacity-50"
    }, /*#__PURE__*/react.createElement("div", {
      className: "transparent flex h-full w-full items-center justify-center"
    }, /*#__PURE__*/react.createElement("p", {
      className: "text-primary-light text-xl font-light"
    }, /*#__PURE__*/react.createElement("h4", null, "Error Loading Image"), /*#__PURE__*/react.createElement("p", null, "An error has occurred."), /*#__PURE__*/react.createElement("p", null, error)))));
  }
  if (loading) {
    return (
      /*#__PURE__*/
      // IMPORTANT: we need to use the pointer-events-none class to prevent the loading indicator from
      // interacting with the mouse, since scrolling should propagate to the viewport underneath
      react.createElement("div", {
        className: "pointer-events-none absolute top-0 left-0 h-full w-full bg-black opacity-50"
      }, /*#__PURE__*/react.createElement("div", {
        className: "transparent flex h-full w-full items-center justify-center"
      }, /*#__PURE__*/react.createElement("p", {
        className: "text-primary-light text-xl font-light"
      }, "Loading...")))
    );
  }
  return null;
}
ViewportImageSliceLoadingIndicator.propTypes = {
  percentComplete: (prop_types_default()).number,
  error: (prop_types_default()).object,
  element: (prop_types_default()).object
};
ViewportImageSliceLoadingIndicator.defaultProps = {
  percentComplete: 0,
  error: null
};
/* harmony default export */ const Overlays_ViewportImageSliceLoadingIndicator = (ViewportImageSliceLoadingIndicator);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/Overlays/CornerstoneOverlays.tsx





function CornerstoneOverlays(props) {
  const {
    viewportId,
    element,
    scrollbarHeight,
    servicesManager
  } = props;
  const {
    cornerstoneViewportService
  } = servicesManager.services;
  const [imageSliceData, setImageSliceData] = (0,react.useState)({
    imageIndex: 0,
    numberOfSlices: 0
  });
  const [viewportData, setViewportData] = (0,react.useState)(null);
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = cornerstoneViewportService.subscribe(cornerstoneViewportService.EVENTS.VIEWPORT_DATA_CHANGED, props => {
      if (props.viewportId !== viewportId) {
        return;
      }
      setViewportData(props.viewportData);
    });
    return () => {
      unsubscribe();
    };
  }, [viewportId]);
  if (!element) {
    return null;
  }
  if (viewportData) {
    const viewportInfo = cornerstoneViewportService.getViewportInfo(viewportId);
    if (viewportInfo?.viewportOptions?.customViewportProps?.hideOverlays) {
      return null;
    }
  }
  return /*#__PURE__*/react.createElement("div", {
    className: "noselect"
  }, /*#__PURE__*/react.createElement(ViewportImageScrollbar, {
    viewportId: viewportId,
    viewportData: viewportData,
    element: element,
    imageSliceData: imageSliceData,
    setImageSliceData: setImageSliceData,
    scrollbarHeight: scrollbarHeight,
    servicesManager: servicesManager
  }), /*#__PURE__*/react.createElement(Overlays_CustomizableViewportOverlay, {
    imageSliceData: imageSliceData,
    viewportData: viewportData,
    viewportId: viewportId,
    servicesManager: servicesManager,
    element: element
  }), /*#__PURE__*/react.createElement(Overlays_ViewportImageSliceLoadingIndicator, {
    viewportData: viewportData,
    element: element
  }), /*#__PURE__*/react.createElement(Overlays_ViewportOrientationMarkers, {
    imageSliceData: imageSliceData,
    element: element,
    viewportData: viewportData,
    servicesManager: servicesManager,
    viewportId: viewportId
  }));
}
/* harmony default export */ const Overlays_CornerstoneOverlays = (CornerstoneOverlays);
// EXTERNAL MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/utils/getSOPInstanceAttributes.js
var getSOPInstanceAttributes = __webpack_require__(87172);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/components/CinePlayer/CinePlayer.tsx



function WrappedCinePlayer(_ref) {
  let {
    enabledVPElement,
    viewportId,
    servicesManager
  } = _ref;
  const {
    toolbarService,
    customizationService
  } = servicesManager.services;
  const [{
    isCineEnabled,
    cines
  }, cineService] = (0,ui_src/* useCine */.vQ)();
  const [{
    activeViewportId
  }] = (0,ui_src/* useViewportGrid */.O_)();
  const {
    component: CinePlayerComponent = ui_src/* CinePlayer */.H6
  } = customizationService.get('cinePlayer') ?? {};
  const handleCineClose = () => {
    toolbarService.recordInteraction({
      groupId: 'MoreTools',
      interactionType: 'toggle',
      commands: [{
        commandName: 'toggleCine',
        commandOptions: {},
        toolName: 'cine',
        context: 'CORNERSTONE'
      }]
    });
  };
  const cineHandler = () => {
    if (!cines || !cines[viewportId] || !enabledVPElement) {
      return;
    }
    const cine = cines[viewportId];
    const isPlaying = cine.isPlaying || false;
    const frameRate = cine.frameRate || 24;
    const validFrameRate = Math.max(frameRate, 1);
    if (isPlaying) {
      cineService.playClip(enabledVPElement, {
        framesPerSecond: validFrameRate
      });
    } else {
      cineService.stopClip(enabledVPElement);
    }
  };
  (0,react.useEffect)(() => {
    dist_esm.eventTarget.addEventListener(dist_esm.Enums.Events.STACK_VIEWPORT_NEW_STACK, cineHandler);
    return () => {
      cineService.setCine({
        id: viewportId,
        isPlaying: false
      });
      dist_esm.eventTarget.removeEventListener(dist_esm.Enums.Events.STACK_VIEWPORT_NEW_STACK, cineHandler);
    };
  }, [enabledVPElement]);
  (0,react.useEffect)(() => {
    if (!cines || !cines[viewportId] || !enabledVPElement) {
      return;
    }
    cineHandler();
    return () => {
      if (enabledVPElement && cines?.[viewportId]?.isPlaying) {
        cineService.stopClip(enabledVPElement);
      }
    };
  }, [cines, viewportId, cineService, enabledVPElement, cineHandler]);
  const cine = cines[viewportId];
  const isPlaying = cine && cine.isPlaying || false;
  return isCineEnabled && /*#__PURE__*/react.createElement(CinePlayerComponent, {
    className: "absolute left-1/2 bottom-3 -translate-x-1/2",
    isPlaying: isPlaying,
    onClose: handleCineClose,
    onPlayPauseChange: isPlaying => cineService.setCine({
      id: activeViewportId,
      isPlaying
    }),
    onFrameRateChange: frameRate => cineService.setCine({
      id: activeViewportId,
      frameRate
    })
  });
}
/* harmony default export */ const CinePlayer = (WrappedCinePlayer);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/components/CinePlayer/index.ts

/* harmony default export */ const components_CinePlayer = (CinePlayer);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/Viewport/OHIFCornerstoneViewport.tsx












const STACK = 'stack';

/**
 * Caches the jump to measurement operation, so that if display set is shown,
 * it can jump to the measurement.
 */
let cacheJumpToMeasurementEvent;
function areEqual(prevProps, nextProps) {
  if (nextProps.needsRerendering) {
    return false;
  }
  if (prevProps.displaySets.length !== nextProps.displaySets.length) {
    return false;
  }
  if (prevProps.viewportOptions.orientation !== nextProps.viewportOptions.orientation) {
    return false;
  }
  if (prevProps.viewportOptions.toolGroupId !== nextProps.viewportOptions.toolGroupId) {
    return false;
  }
  if (prevProps.viewportOptions.viewportType !== nextProps.viewportOptions.viewportType) {
    return false;
  }
  if (nextProps.viewportOptions.needsRerendering) {
    return false;
  }
  const prevDisplaySets = prevProps.displaySets;
  const nextDisplaySets = nextProps.displaySets;
  if (prevDisplaySets.length !== nextDisplaySets.length) {
    return false;
  }
  for (let i = 0; i < prevDisplaySets.length; i++) {
    const prevDisplaySet = prevDisplaySets[i];
    const foundDisplaySet = nextDisplaySets.find(nextDisplaySet => nextDisplaySet.displaySetInstanceUID === prevDisplaySet.displaySetInstanceUID);
    if (!foundDisplaySet) {
      return false;
    }

    // check they contain the same image
    if (foundDisplaySet.images?.length !== prevDisplaySet.images?.length) {
      return false;
    }

    // check if their imageIds are the same
    if (foundDisplaySet.images?.length) {
      for (let j = 0; j < foundDisplaySet.images.length; j++) {
        if (foundDisplaySet.images[j].imageId !== prevDisplaySet.images[j].imageId) {
          return false;
        }
      }
    }
  }
  return true;
}

// Todo: This should be done with expose of internal API similar to react-vtkjs-viewport
// Then we don't need to worry about the re-renders if the props change.
const OHIFCornerstoneViewport = /*#__PURE__*/react.memo(props => {
  const {
    displaySets,
    dataSource,
    viewportOptions,
    displaySetOptions,
    servicesManager,
    commandsManager,
    onElementEnabled,
    onElementDisabled,
    isJumpToMeasurementDisabled,
    // Note: you SHOULD NOT use the initialImageIdOrIndex for manipulation
    // of the imageData in the OHIFCornerstoneViewport. This prop is used
    // to set the initial state of the viewport's first image to render
    initialImageIndex
  } = props;
  const viewportId = viewportOptions.viewportId;
  const [scrollbarHeight, setScrollbarHeight] = (0,react.useState)('100px');
  const [enabledVPElement, setEnabledVPElement] = (0,react.useState)(null);
  const elementRef = (0,react.useRef)();
  const {
    measurementService,
    displaySetService,
    toolbarService,
    toolGroupService,
    syncGroupService,
    cornerstoneViewportService,
    cornerstoneCacheService,
    viewportGridService,
    stateSyncService
  } = servicesManager.services;
  const [viewportDialogState] = (0,ui_src/* useViewportDialog */.en)();
  // useCallback for scroll bar height calculation
  const setImageScrollBarHeight = (0,react.useCallback)(() => {
    const scrollbarHeight = `${elementRef.current.clientHeight - 20}px`;
    setScrollbarHeight(scrollbarHeight);
  }, [elementRef]);

  // useCallback for onResize
  const onResize = (0,react.useCallback)(() => {
    if (elementRef.current) {
      cornerstoneViewportService.resize();
      setImageScrollBarHeight();
    }
  }, [elementRef]);
  const cleanUpServices = (0,react.useCallback)(viewportInfo => {
    const renderingEngineId = viewportInfo.getRenderingEngineId();
    const syncGroups = viewportInfo.getSyncGroups();
    toolGroupService.removeViewportFromToolGroup(viewportId, renderingEngineId);
    syncGroupService.removeViewportFromSyncGroup(viewportId, renderingEngineId, syncGroups);
  }, [viewportId]);
  const elementEnabledHandler = (0,react.useCallback)(evt => {
    // check this is this element reference and return early if doesn't match
    if (evt.detail.element !== elementRef.current) {
      return;
    }
    const {
      viewportId,
      element
    } = evt.detail;
    const viewportInfo = cornerstoneViewportService.getViewportInfo(viewportId);
    (0,state/* setEnabledElement */.Yc)(viewportId, element);
    setEnabledVPElement(element);
    const renderingEngineId = viewportInfo.getRenderingEngineId();
    const toolGroupId = viewportInfo.getToolGroupId();
    const syncGroups = viewportInfo.getSyncGroups();
    toolGroupService.addViewportToToolGroup(viewportId, renderingEngineId, toolGroupId);
    syncGroupService.addViewportToSyncGroup(viewportId, renderingEngineId, syncGroups);
    if (onElementEnabled) {
      onElementEnabled(evt);
    }
  }, [viewportId, onElementEnabled, toolGroupService]);

  // disable the element upon unmounting
  (0,react.useEffect)(() => {
    cornerstoneViewportService.enableViewport(viewportId, elementRef.current);
    dist_esm.eventTarget.addEventListener(dist_esm.Enums.Events.ELEMENT_ENABLED, elementEnabledHandler);
    setImageScrollBarHeight();
    return () => {
      const viewportInfo = cornerstoneViewportService.getViewportInfo(viewportId);
      if (!viewportInfo) {
        return;
      }
      cleanUpServices(viewportInfo);
      cornerstoneViewportService.storePresentation({
        viewportId
      });
      if (onElementDisabled) {
        onElementDisabled(viewportInfo);
      }
      dist_esm.eventTarget.removeEventListener(dist_esm.Enums.Events.ELEMENT_ENABLED, elementEnabledHandler);
    };
  }, []);

  // subscribe to displaySet metadata invalidation (updates)
  // Currently, if the metadata changes we need to re-render the display set
  // for it to take effect in the viewport. As we deal with scaling in the loading,
  // we need to remove the old volume from the cache, and let the
  // viewport to re-add it which will use the new metadata. Otherwise, the
  // viewport will use the cached volume and the new metadata will not be used.
  // Note: this approach does not actually end of sending network requests
  // and it uses the network cache
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = displaySetService.subscribe(displaySetService.EVENTS.DISPLAY_SET_SERIES_METADATA_INVALIDATED, async _ref => {
      let {
        displaySetInstanceUID: invalidatedDisplaySetInstanceUID,
        invalidateData
      } = _ref;
      if (!invalidateData) {
        return;
      }
      const viewportInfo = cornerstoneViewportService.getViewportInfo(viewportId);
      if (viewportInfo.hasDisplaySet(invalidatedDisplaySetInstanceUID)) {
        const viewportData = viewportInfo.getViewportData();
        const newViewportData = await cornerstoneCacheService.invalidateViewportData(viewportData, invalidatedDisplaySetInstanceUID, dataSource, displaySetService);
        const keepCamera = true;
        cornerstoneViewportService.updateViewport(viewportId, newViewportData, keepCamera);
      }
    });
    return () => {
      unsubscribe();
    };
  }, [viewportId]);
  (0,react.useEffect)(() => {
    // handle the default viewportType to be stack
    if (!viewportOptions.viewportType) {
      viewportOptions.viewportType = STACK;
    }
    const loadViewportData = async () => {
      const viewportData = await cornerstoneCacheService.createViewportData(displaySets, viewportOptions, dataSource, initialImageIndex);

      // The presentation state will have been stored previously by closing
      // a viewport.  Otherwise, this viewport will be unchanged and the
      // presentation information will be directly carried over.
      const {
        lutPresentationStore,
        positionPresentationStore
      } = stateSyncService.getState();
      const {
        presentationIds
      } = viewportOptions;
      const presentations = {
        positionPresentation: positionPresentationStore[presentationIds?.positionPresentationId],
        lutPresentation: lutPresentationStore[presentationIds?.lutPresentationId]
      };
      let measurement;
      if (cacheJumpToMeasurementEvent?.viewportId === viewportId) {
        measurement = cacheJumpToMeasurementEvent.measurement;
        // Delete the position presentation so that viewport navigates direct
        presentations.positionPresentation = null;
        cacheJumpToMeasurementEvent = null;
      }

      // Note: This is a hack to get the grid to re-render the OHIFCornerstoneViewport component
      // Used for segmentation hydration right now, since the logic to decide whether
      // a viewport needs to render a segmentation lives inside the CornerstoneViewportService
      // so we need to re-render (force update via change of the needsRerendering) so that React
      // does the diffing and decides we should render this again (although the id and element has not changed)
      // so that the CornerstoneViewportService can decide whether to render the segmentation or not. Not that we reached here we can turn it off.
      if (viewportOptions.needsRerendering) {
        viewportOptions.needsRerendering = false;
      }
      cornerstoneViewportService.setViewportData(viewportId, viewportData, viewportOptions, displaySetOptions, presentations);
      if (measurement) {
        esm.annotation.selection.setAnnotationSelected(measurement.uid);
      }
    };
    loadViewportData();
  }, [viewportOptions, displaySets, dataSource]);

  /**
   * There are two scenarios for jump to click
   * 1. Current viewports contain the displaySet that the annotation was drawn on
   * 2. Current viewports don't contain the displaySet that the annotation was drawn on
   * and we need to change the viewports displaySet for jumping.
   * Since measurement_jump happens via events and listeners, the former case is handled
   * by the measurement_jump direct callback, but the latter case is handled first by
   * the viewportGrid to set the correct displaySet on the viewport, AND THEN we check
   * the cache for jumping to see if there is any jump queued, then we jump to the correct slice.
   */
  (0,react.useEffect)(() => {
    if (isJumpToMeasurementDisabled) {
      return;
    }
    const unsubscribeFromJumpToMeasurementEvents = _subscribeToJumpToMeasurementEvents(measurementService, displaySetService, elementRef, viewportId, displaySets, viewportGridService, cornerstoneViewportService);
    _checkForCachedJumpToMeasurementEvents(measurementService, displaySetService, elementRef, viewportId, displaySets, viewportGridService, cornerstoneViewportService);
    return () => {
      unsubscribeFromJumpToMeasurementEvents();
    };
  }, [displaySets, elementRef, viewportId]);
  return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("div", {
    className: "viewport-wrapper"
  }, /*#__PURE__*/react.createElement(index_esm/* default */.ZP, {
    refreshMode: "debounce",
    refreshRate: 50 // Wait 50 ms after last move to render
    ,
    onResize: onResize,
    targetRef: elementRef.current
  }), /*#__PURE__*/react.createElement("div", {
    className: "cornerstone-viewport-element",
    style: {
      height: '100%',
      width: '100%'
    },
    onContextMenu: e => e.preventDefault(),
    onMouseDown: e => e.preventDefault(),
    ref: elementRef
  }), /*#__PURE__*/react.createElement(Overlays_CornerstoneOverlays, {
    viewportId: viewportId,
    toolBarService: toolbarService,
    element: elementRef.current,
    scrollbarHeight: scrollbarHeight,
    servicesManager: servicesManager
  }), /*#__PURE__*/react.createElement(components_CinePlayer, {
    enabledVPElement: enabledVPElement,
    viewportId: viewportId,
    servicesManager: servicesManager
  })), /*#__PURE__*/react.createElement("div", {
    className: "absolute w-full"
  }, viewportDialogState.viewportId === viewportId && /*#__PURE__*/react.createElement(ui_src/* Notification */.P_, {
    id: "viewport-notification",
    message: viewportDialogState.message,
    type: viewportDialogState.type,
    actions: viewportDialogState.actions,
    onSubmit: viewportDialogState.onSubmit,
    onOutsideClick: viewportDialogState.onOutsideClick
  })));
}, areEqual);
function _subscribeToJumpToMeasurementEvents(measurementService, displaySetService, elementRef, viewportId, displaySets, viewportGridService, cornerstoneViewportService) {
  const {
    unsubscribe
  } = measurementService.subscribe(src.MeasurementService.EVENTS.JUMP_TO_MEASUREMENT_VIEWPORT, props => {
    cacheJumpToMeasurementEvent = props;
    const {
      viewportId: jumpId,
      measurement,
      isConsumed
    } = props;
    if (!measurement || isConsumed) {
      return;
    }
    if (cacheJumpToMeasurementEvent.cornerstoneViewport === undefined) {
      // Decide on which viewport should handle this
      cacheJumpToMeasurementEvent.cornerstoneViewport = cornerstoneViewportService.getViewportIdToJump(jumpId, measurement.displaySetInstanceUID, {
        referencedImageId: measurement.referencedImageId
      });
    }
    if (cacheJumpToMeasurementEvent.cornerstoneViewport !== viewportId) {
      return;
    }
    _jumpToMeasurement(measurement, elementRef, viewportId, measurementService, displaySetService, viewportGridService, cornerstoneViewportService);
  });
  return unsubscribe;
}

// Check if there is a queued jumpToMeasurement event
function _checkForCachedJumpToMeasurementEvents(measurementService, displaySetService, elementRef, viewportId, displaySets, viewportGridService, cornerstoneViewportService) {
  if (!cacheJumpToMeasurementEvent) {
    return;
  }
  if (cacheJumpToMeasurementEvent.isConsumed) {
    cacheJumpToMeasurementEvent = null;
    return;
  }
  const displaysUIDs = displaySets.map(displaySet => displaySet.displaySetInstanceUID);
  if (!displaysUIDs?.length) {
    return;
  }

  // Jump to measurement if the measurement exists
  const {
    measurement
  } = cacheJumpToMeasurementEvent;
  if (measurement && elementRef) {
    if (displaysUIDs.includes(measurement?.displaySetInstanceUID)) {
      _jumpToMeasurement(measurement, elementRef, viewportId, measurementService, displaySetService, viewportGridService, cornerstoneViewportService);
    }
  }
}
function _jumpToMeasurement(measurement, targetElementRef, viewportId, measurementService, displaySetService, viewportGridService, cornerstoneViewportService) {
  const targetElement = targetElementRef.current;
  const {
    displaySetInstanceUID,
    SOPInstanceUID,
    frameNumber
  } = measurement;
  if (!SOPInstanceUID) {
    console.warn('cannot jump in a non-acquisition plane measurements yet');
    return;
  }
  const referencedDisplaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);

  // Todo: setCornerstoneMeasurementActive should be handled by the toolGroupManager
  //  to set it properly
  // setCornerstoneMeasurementActive(measurement);

  viewportGridService.setActiveViewportId(viewportId);
  const enabledElement = (0,dist_esm.getEnabledElement)(targetElement);
  if (enabledElement) {
    // See how the jumpToSlice() of Cornerstone3D deals with imageIdx param.
    const viewport = enabledElement.viewport;
    let imageIdIndex = 0;
    let viewportCameraDirectionMatch = true;
    if (viewport instanceof dist_esm.StackViewport) {
      const imageIds = viewport.getImageIds();
      imageIdIndex = imageIds.findIndex(imageId => {
        const {
          SOPInstanceUID: aSOPInstanceUID,
          frameNumber: aFrameNumber
        } = (0,getSOPInstanceAttributes/* default */.Z)(imageId);
        return aSOPInstanceUID === SOPInstanceUID && (!frameNumber || frameNumber === aFrameNumber);
      });
    } else {
      // for volume viewport we can't rely on the imageIdIndex since it can be
      // a reconstructed view that doesn't match the original slice numbers etc.
      const {
        viewPlaneNormal: measurementViewPlane
      } = measurement.metadata;
      imageIdIndex = referencedDisplaySet.images.findIndex(i => i.SOPInstanceUID === SOPInstanceUID);
      const {
        viewPlaneNormal: viewportViewPlane
      } = viewport.getCamera();

      // should compare abs for both planes since the direction can be flipped
      if (measurementViewPlane && !dist_esm.utilities.isEqual(measurementViewPlane.map(Math.abs), viewportViewPlane.map(Math.abs))) {
        viewportCameraDirectionMatch = false;
      }
    }
    if (!viewportCameraDirectionMatch || imageIdIndex === -1) {
      return;
    }
    esm.utilities.jumpToSlice(targetElement, {
      imageIndex: imageIdIndex
    });
    esm.annotation.selection.setAnnotationSelected(measurement.uid);
    // Jump to measurement consumed, remove.
    cacheJumpToMeasurementEvent?.consume?.();
    cacheJumpToMeasurementEvent = null;
  }
}

// Component displayName
OHIFCornerstoneViewport.displayName = 'OHIFCornerstoneViewport';
OHIFCornerstoneViewport.defaultProps = {
  isJumpToMeasurementDisabled: false
};
OHIFCornerstoneViewport.propTypes = {
  displaySets: (prop_types_default()).array.isRequired,
  dataSource: (prop_types_default()).object.isRequired,
  viewportOptions: (prop_types_default()).object,
  displaySetOptions: prop_types_default().arrayOf((prop_types_default()).any),
  servicesManager: (prop_types_default()).object.isRequired,
  onElementEnabled: (prop_types_default()).func,
  isJumpToMeasurementDisabled: (prop_types_default()).bool,
  // Note: you SHOULD NOT use the initialImageIdOrIndex for manipulation
  // of the imageData in the OHIFCornerstoneViewport. This prop is used
  // to set the initial state of the viewport's first image to render
  initialImageIdOrIndex: prop_types_default().oneOfType([(prop_types_default()).string, (prop_types_default()).number])
};
/* harmony default export */ const Viewport_OHIFCornerstoneViewport = (OHIFCornerstoneViewport);

/***/ })

}]);