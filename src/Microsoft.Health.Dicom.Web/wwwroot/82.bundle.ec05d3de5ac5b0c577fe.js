(self["webpackChunk"] = self["webpackChunk"] || []).push([[82],{

/***/ 78227:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  Types: () => (/* reexport */ types_namespaceObject),
  "default": () => (/* binding */ cornerstone_src),
  getActiveViewportEnabledElement: () => (/* reexport */ getActiveViewportEnabledElement),
  measurementMappingUtils: () => (/* reexport */ utils_namespaceObject),
  toolNames: () => (/* reexport */ toolNames)
});

// NAMESPACE OBJECT: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/utils/index.ts
var utils_namespaceObject = {};
__webpack_require__.r(utils_namespaceObject);
__webpack_require__.d(utils_namespaceObject, {
  getDisplayUnit: () => (utils_getDisplayUnit),
  getFirstAnnotationSelected: () => (getFirstAnnotationSelected),
  getHandlesFromPoints: () => (getHandlesFromPoints),
  getSOPInstanceAttributes: () => (getSOPInstanceAttributes/* default */.Z),
  isAnnotationSelected: () => (isAnnotationSelected),
  setAnnotationSelected: () => (setAnnotationSelected)
});

// NAMESPACE OBJECT: ../../../extensions/cornerstone/src/types/index.ts
var types_namespaceObject = {};
__webpack_require__.r(types_namespaceObject);

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var esm = __webpack_require__(3743);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/tools/dist/esm/index.js + 348 modules
var dist_esm = __webpack_require__(14957);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/streaming-image-volume-loader/dist/esm/index.js + 13 modules
var streaming_image_volume_loader_dist_esm = __webpack_require__(7087);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/dicom-image-loader/dist/dynamic-import/cornerstoneDICOMImageLoader.min.js
var cornerstoneDICOMImageLoader_min = __webpack_require__(61539);
var cornerstoneDICOMImageLoader_min_default = /*#__PURE__*/__webpack_require__.n(cornerstoneDICOMImageLoader_min);
// EXTERNAL MODULE: ../../../node_modules/dicom-parser/dist/dicomParser.min.js
var dicomParser_min = __webpack_require__(56660);
var dicomParser_min_default = /*#__PURE__*/__webpack_require__.n(dicomParser_min);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/initWADOImageLoader.js






const {
  registerVolumeLoader
} = esm.volumeLoader;
let initialized = false;
function initWebWorkers(appConfig) {
  const config = {
    maxWebWorkers: Math.min(Math.max(navigator.hardwareConcurrency - 1, 1), appConfig.maxNumberOfWebWorkers),
    startWebWorkersOnDemand: true,
    taskConfiguration: {
      decodeTask: {
        initializeCodecsOnStartup: false,
        usePDFJS: false,
        strict: false
      }
    }
  };
  if (!initialized) {
    cornerstoneDICOMImageLoader_min_default().webWorkerManager.initialize(config);
    initialized = true;
  }
}
function initWADOImageLoader(userAuthenticationService, appConfig, extensionManager) {
  (cornerstoneDICOMImageLoader_min_default()).external.cornerstone = esm;
  (cornerstoneDICOMImageLoader_min_default()).external.dicomParser = (dicomParser_min_default());
  registerVolumeLoader('cornerstoneStreamingImageVolume', streaming_image_volume_loader_dist_esm/* cornerstoneStreamingImageVolumeLoader */.IU);
  cornerstoneDICOMImageLoader_min_default().configure({
    decodeConfig: {
      // !! IMPORTANT !!
      // We should set this flag to false, since, by default @cornerstonejs/dicom-image-loader
      // will convert everything to integers (to be able to work with cornerstone-2d).
      // Until the default is set to true (which is the case for cornerstone3D),
      // we should set this flag to false.
      convertFloatPixelDataToInt: false,
      use16BitDataType: Boolean(appConfig.use16BitDataType)
    },
    beforeSend: function (xhr) {
      //TODO should be removed in the future and request emitted by DicomWebDataSource
      const sourceConfig = extensionManager.getActiveDataSource()?.[0].getConfig() ?? {};
      const headers = userAuthenticationService.getAuthorizationHeader();
      const acceptHeader = src.utils.generateAcceptHeader(sourceConfig.acceptHeader, sourceConfig.requestTransferSyntaxUID, sourceConfig.omitQuotationForMultipartRequest);
      const xhrRequestHeaders = {
        Accept: acceptHeader
      };
      if (headers) {
        Object.assign(xhrRequestHeaders, headers);
      }
      return xhrRequestHeaders;
    },
    errorInterceptor: error => {
      src/* errorHandler */.Po.getHTTPErrorHandler(error);
    }
  });
  initWebWorkers(appConfig);
}
function destroy() {
  // Note: we don't want to call .terminate on the webWorkerManager since
  // that resets the config
  const webWorkers = webWorkerManager.webWorkers;
  for (let i = 0; i < webWorkers.length; i++) {
    webWorkers[i].worker.terminate();
  }
  webWorkers.length = 0;
}
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/callInputDialog.tsx



/**
 *
 * @param {*} data
 * @param {*} data.text
 * @param {*} data.label
 * @param {*} event
 * @param {*} callback
 * @param {*} isArrowAnnotateInputDialog
 * @param {*} dialogConfig
 * @param {string?} dialogConfig.dialogTitle - title of the input dialog
 * @param {string?} dialogConfig.inputLabel - show label above the input
 */
function callInputDialog(uiDialogService, data, callback) {
  let isArrowAnnotateInputDialog = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : true;
  let dialogConfig = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : {};
  const dialogId = 'dialog-enter-annotation';
  const label = data ? isArrowAnnotateInputDialog ? data.text : data.label : '';
  const {
    dialogTitle = 'Annotation',
    inputLabel = 'Enter your annotation',
    validateFunc = value => true
  } = dialogConfig;
  const onSubmitHandler = _ref => {
    let {
      action,
      value
    } = _ref;
    switch (action.id) {
      case 'save':
        if (typeof validateFunc === 'function' && !validateFunc(value.label)) {
          return;
        }
        callback(value.label, action.id);
        break;
      case 'cancel':
        callback('', action.id);
        break;
    }
    uiDialogService.dismiss({
      id: dialogId
    });
  };
  if (uiDialogService) {
    uiDialogService.create({
      id: dialogId,
      centralize: true,
      isDraggable: false,
      showOverlay: true,
      content: ui_src/* Dialog */.Vq,
      contentProps: {
        title: dialogTitle,
        value: {
          label
        },
        noCloseButton: true,
        onClose: () => uiDialogService.dismiss({
          id: dialogId
        }),
        actions: [{
          id: 'cancel',
          text: 'Cancel',
          type: ui_src/* ButtonEnums.type */.LZ.dt.secondary
        }, {
          id: 'save',
          text: 'Save',
          type: ui_src/* ButtonEnums.type */.LZ.dt.primary
        }],
        onSubmit: onSubmitHandler,
        body: _ref2 => {
          let {
            value,
            setValue
          } = _ref2;
          return /*#__PURE__*/react.createElement(ui_src/* Input */.II, {
            autoFocus: true,
            className: "border-primary-main bg-black",
            type: "text",
            id: "annotation",
            label: inputLabel,
            labelClassName: "text-white text-[14px] leading-[1.2]",
            value: value.label,
            onChange: event => {
              event.persist();
              setValue(value => ({
                ...value,
                label: event.target.value
              }));
            },
            onKeyPress: event => {
              if (event.key === 'Enter') {
                onSubmitHandler({
                  value,
                  action: {
                    id: 'save'
                  }
                });
              }
            }
          });
        }
      }
    });
  }
}
/* harmony default export */ const utils_callInputDialog = (callInputDialog);
// EXTERNAL MODULE: ../../../extensions/cornerstone/src/state.ts
var state = __webpack_require__(73704);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/getActiveViewportEnabledElement.ts


function getActiveViewportEnabledElement(viewportGridService) {
  const {
    activeViewportId
  } = viewportGridService.getState();
  const {
    element
  } = (0,state/* getEnabledElement */.K8)(activeViewportId) || {};
  const enabledElement = (0,esm.getEnabledElement)(element);
  return enabledElement;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/tools/CalibrationLineTool.ts




const {
  calibrateImageSpacing
} = dist_esm.utilities;

/**
 * Calibration Line tool works almost the same as the
 */
class CalibrationLineTool extends dist_esm.LengthTool {
  constructor() {
    super(...arguments);
    this._renderingViewport = void 0;
    this._lengthToolRenderAnnotation = this.renderAnnotation;
    this.renderAnnotation = (enabledElement, svgDrawingHelper) => {
      const {
        viewport
      } = enabledElement;
      this._renderingViewport = viewport;
      return this._lengthToolRenderAnnotation(enabledElement, svgDrawingHelper);
    };
  }
  _getTextLines(data, targetId) {
    const [canvasPoint1, canvasPoint2] = data.handles.points.map(p => this._renderingViewport.worldToCanvas(p));
    // for display, round to 2 decimal points
    const lengthPx = Math.round(calculateLength2(canvasPoint1, canvasPoint2) * 100) / 100;
    const textLines = [`${lengthPx}px`];
    return textLines;
  }
}
CalibrationLineTool.toolName = 'CalibrationLine';
function calculateLength2(point1, point2) {
  const dx = point1[0] - point2[0];
  const dy = point1[1] - point2[1];
  return Math.sqrt(dx * dx + dy * dy);
}
function calculateLength3(pos1, pos2) {
  const dx = pos1[0] - pos2[0];
  const dy = pos1[1] - pos2[1];
  const dz = pos1[2] - pos2[2];
  return Math.sqrt(dx * dx + dy * dy + dz * dz);
}
/* harmony default export */ const tools_CalibrationLineTool = (CalibrationLineTool);
function onCompletedCalibrationLine(servicesManager, csToolsEvent) {
  const {
    uiDialogService,
    viewportGridService
  } = servicesManager.services;

  // calculate length (mm) with the current Pixel Spacing
  const annotationAddedEventDetail = csToolsEvent.detail;
  const {
    annotation: {
      metadata,
      data: annotationData
    }
  } = annotationAddedEventDetail;
  const {
    referencedImageId: imageId
  } = metadata;
  const enabledElement = getActiveViewportEnabledElement(viewportGridService);
  const {
    viewport
  } = enabledElement;
  const length = Math.round(calculateLength3(annotationData.handles.points[0], annotationData.handles.points[1]) * 100) / 100;

  // calculate the currently applied pixel spacing on the viewport
  const calibratedPixelSpacing = esm.metaData.get('calibratedPixelSpacing', imageId);
  const imagePlaneModule = esm.metaData.get('imagePlaneModule', imageId);
  const currentRowPixelSpacing = calibratedPixelSpacing?.[0] || imagePlaneModule?.rowPixelSpacing || 1;
  const currentColumnPixelSpacing = calibratedPixelSpacing?.[1] || imagePlaneModule?.columnPixelSpacing || 1;
  const adjustCalibration = newLength => {
    const spacingScale = newLength / length;

    // trigger resize of the viewport to adjust the world/pixel mapping
    calibrateImageSpacing(imageId, viewport.getRenderingEngine(), {
      type: 'User',
      scale: 1 / spacingScale
    });
  };
  return new Promise((resolve, reject) => {
    if (!uiDialogService) {
      reject('UIDialogService is not initiated');
      return;
    }
    utils_callInputDialog(uiDialogService, {
      text: '',
      label: `${length}`
    }, (value, id) => {
      if (id === 'save') {
        adjustCalibration(Number.parseFloat(value));
        resolve(true);
      } else {
        reject('cancel');
      }
    }, false, {
      dialogTitle: 'Calibration',
      inputLabel: 'Actual Physical distance (mm)',
      // the input value must be a number
      validateFunc: val => {
        try {
          const v = Number.parseFloat(val);
          return !isNaN(v) && v !== 0.0;
        } catch {
          return false;
        }
      }
    });
  });
}
// EXTERNAL MODULE: ../../core/src/utils/index.js + 25 modules
var utils = __webpack_require__(77250);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/tools/ImageOverlayViewerTool.tsx




/**
 * Image Overlay Viewer tool is not a traditional tool that requires user interactin.
 * But it is used to display Pixel Overlays. And it will provide toggling capability.
 *
 * The documentation for Overlay Plane Module of DICOM can be found in [C.9.2 of
 * Part-3 of DICOM standard](https://dicom.nema.org/medical/dicom/2018b/output/chtml/part03/sect_C.9.2.html)
 *
 * Image Overlay rendered by this tool can be toggled on and off using
 * toolGroup.setToolEnabled() and toolGroup.setToolDisabled()
 */
class ImageOverlayViewerTool extends dist_esm.AnnotationDisplayTool {
  constructor() {
    let toolProps = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
    let defaultToolProps = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {
      supportedInteractionTypes: [],
      configuration: {
        fillColor: [255, 127, 127, 255]
      }
    };
    super(toolProps, defaultToolProps);
    this._cachedOverlayMetadata = new Map();
    this._cachedStats = {};
    this.onSetToolDisabled = () => {
      this._cachedStats = {};
      this._cachedOverlayMetadata = new Map();
    };
    this.renderAnnotation = (enabledElement, svgDrawingHelper) => {
      const {
        viewport
      } = enabledElement;
      const imageId = this.getReferencedImageId(viewport);
      if (!imageId) {
        return;
      }
      const overlays = this._cachedOverlayMetadata.get(imageId) ?? esm.metaData.get('overlayPlaneModule', imageId)?.overlays;

      // no overlays
      if (!overlays?.length) {
        return;
      }
      this._cachedOverlayMetadata.set(imageId, overlays);
      this._getCachedStat(imageId, overlays, this.configuration.fillColor).then(cachedStat => {
        cachedStat.overlays.forEach(overlay => {
          this._renderOverlay(enabledElement, svgDrawingHelper, overlay);
        });
      });
      return true;
    };
  }
  getReferencedImageId(viewport) {
    if (viewport instanceof esm.VolumeViewport) {
      return;
    }
    const targetId = this.getTargetId(viewport);
    return targetId.split('imageId:')[1];
  }
  /**
   * Render to DOM
   *
   * @param enabledElement
   * @param svgDrawingHelper
   * @param overlayData
   * @returns
   */
  _renderOverlay(enabledElement, svgDrawingHelper, overlayData) {
    const {
      viewport
    } = enabledElement;
    const imageId = this.getReferencedImageId(viewport);
    if (!imageId) {
      return;
    }

    // Decide the rendering position of the overlay image on the current canvas
    const {
      _id,
      columns: width,
      rows: height,
      x,
      y
    } = overlayData;
    const overlayTopLeftWorldPos = esm.utilities.imageToWorldCoords(imageId, [x - 1,
    // Remind that top-left corner's (x, y) is be (1, 1)
    y - 1]);
    const overlayTopLeftOnCanvas = viewport.worldToCanvas(overlayTopLeftWorldPos);
    const overlayBottomRightWorldPos = esm.utilities.imageToWorldCoords(imageId, [width, height]);
    const overlayBottomRightOnCanvas = viewport.worldToCanvas(overlayBottomRightWorldPos);

    // add image to the annotations svg layer
    const svgns = 'http://www.w3.org/2000/svg';
    const svgNodeHash = `image-overlay-${_id}`;
    const existingImageElement = svgDrawingHelper.getSvgNode(svgNodeHash);
    const attributes = {
      'data-id': svgNodeHash,
      width: overlayBottomRightOnCanvas[0] - overlayTopLeftOnCanvas[0],
      height: overlayBottomRightOnCanvas[1] - overlayTopLeftOnCanvas[1],
      x: overlayTopLeftOnCanvas[0],
      y: overlayTopLeftOnCanvas[1],
      href: overlayData.dataUrl
    };
    if (isNaN(attributes.x) || isNaN(attributes.y) || isNaN(attributes.width) || isNaN(attributes.height)) {
      console.warn('Invalid rendering attribute for image overlay', attributes['data-id']);
      return false;
    }
    if (existingImageElement) {
      dist_esm.drawing.setAttributesIfNecessary(attributes, existingImageElement);
      svgDrawingHelper.setNodeTouched(svgNodeHash);
    } else {
      const newImageElement = document.createElementNS(svgns, 'image');
      dist_esm.drawing.setNewAttributesIfValid(attributes, newImageElement);
      svgDrawingHelper.appendNode(newImageElement, svgNodeHash);
    }
    return true;
  }
  async _getCachedStat(imageId, overlayMetadata, color) {
    if (this._cachedStats[imageId] && this._isSameColor(this._cachedStats[imageId].color, color)) {
      return this._cachedStats[imageId];
    }
    const overlays = await Promise.all(overlayMetadata.filter(overlay => overlay.pixelData).map(async (overlay, idx) => {
      let pixelData = null;
      if (overlay.pixelData.Value) {
        pixelData = overlay.pixelData.Value;
      } else if (overlay.pixelData instanceof Array) {
        pixelData = overlay.pixelData[0];
      } else if (overlay.pixelData.retrieveBulkData) {
        pixelData = await overlay.pixelData.retrieveBulkData();
      }
      if (!pixelData) {
        return;
      }
      const dataUrl = this._renderOverlayToDataUrl({
        width: overlay.columns,
        height: overlay.rows
      }, color, pixelData);
      return {
        ...overlay,
        _id: (0,utils/* guid */.M8)(),
        dataUrl,
        // this will be a data url expression of the rendered image
        color
      };
    }));
    this._cachedStats[imageId] = {
      color: color,
      overlays: overlays.filter(overlay => overlay)
    };
    return this._cachedStats[imageId];
  }

  /**
   * compare two RGBA expression of colors.
   *
   * @param color1
   * @param color2
   * @returns
   */
  _isSameColor(color1, color2) {
    return color1 && color2 && color1[0] === color2[0] && color1[1] === color2[1] && color1[2] === color2[2] && color1[3] === color2[3];
  }

  /**
   * pixelData of overlayPlane module is an array of bits corresponding
   * to each of the underlying pixels of the image.
   * Let's create pixel data from bit array of overlay data
   *
   * @param pixelDataRaw
   * @param color
   * @returns
   */
  _renderOverlayToDataUrl(_ref, color, pixelDataRaw) {
    let {
      width,
      height
    } = _ref;
    const pixelDataView = new DataView(pixelDataRaw);
    const totalBits = width * height;
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, width, height); // make it transparent
    ctx.globalCompositeOperation = 'copy';
    const imageData = ctx.getImageData(0, 0, width, height);
    const data = imageData.data;
    for (let i = 0, bitIdx = 0, byteIdx = 0; i < totalBits; i++) {
      if (pixelDataView.getUint8(byteIdx) & 1 << bitIdx) {
        data[i * 4] = color[0];
        data[i * 4 + 1] = color[1];
        data[i * 4 + 2] = color[2];
        data[i * 4 + 3] = color[3];
      }

      // next bit, byte
      if (bitIdx >= 7) {
        bitIdx = 0;
        byteIdx++;
      } else {
        bitIdx++;
      }
    }
    ctx.putImageData(imageData, 0, 0);
    return canvas.toDataURL();
  }
}
ImageOverlayViewerTool.toolName = 'ImageOverlayViewer';
/* harmony default export */ const tools_ImageOverlayViewerTool = (ImageOverlayViewerTool);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/initCornerstoneTools.js



function initCornerstoneTools() {
  let configuration = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
  dist_esm.CrosshairsTool.isAnnotation = false;
  dist_esm.ReferenceLinesTool.isAnnotation = false;
  (0,dist_esm.init)(configuration);
  (0,dist_esm.addTool)(dist_esm.PanTool);
  (0,dist_esm.addTool)(dist_esm.WindowLevelTool);
  (0,dist_esm.addTool)(dist_esm.StackScrollMouseWheelTool);
  (0,dist_esm.addTool)(dist_esm.StackScrollTool);
  (0,dist_esm.addTool)(dist_esm.ZoomTool);
  (0,dist_esm.addTool)(dist_esm.ProbeTool);
  (0,dist_esm.addTool)(dist_esm.VolumeRotateMouseWheelTool);
  (0,dist_esm.addTool)(dist_esm.MIPJumpToClickTool);
  (0,dist_esm.addTool)(dist_esm.LengthTool);
  (0,dist_esm.addTool)(dist_esm.RectangleROITool);
  (0,dist_esm.addTool)(dist_esm.EllipticalROITool);
  (0,dist_esm.addTool)(dist_esm.CircleROITool);
  (0,dist_esm.addTool)(dist_esm.BidirectionalTool);
  (0,dist_esm.addTool)(dist_esm.ArrowAnnotateTool);
  (0,dist_esm.addTool)(dist_esm.DragProbeTool);
  (0,dist_esm.addTool)(dist_esm.AngleTool);
  (0,dist_esm.addTool)(dist_esm.CobbAngleTool);
  (0,dist_esm.addTool)(dist_esm.PlanarFreehandROITool);
  (0,dist_esm.addTool)(dist_esm.MagnifyTool);
  (0,dist_esm.addTool)(dist_esm.CrosshairsTool);
  (0,dist_esm.addTool)(dist_esm.SegmentationDisplayTool);
  (0,dist_esm.addTool)(dist_esm.ReferenceLinesTool);
  (0,dist_esm.addTool)(tools_CalibrationLineTool);
  (0,dist_esm.addTool)(dist_esm.TrackballRotateTool);
  (0,dist_esm.addTool)(dist_esm.CircleScissorsTool);
  (0,dist_esm.addTool)(dist_esm.RectangleScissorsTool);
  (0,dist_esm.addTool)(dist_esm.SphereScissorsTool);
  (0,dist_esm.addTool)(tools_ImageOverlayViewerTool);

  // Modify annotation tools to use dashed lines on SR
  const annotationStyle = {
    textBoxFontSize: '15px',
    lineWidth: '1.5'
  };
  const defaultStyles = dist_esm.annotation.config.style.getDefaultToolStyles();
  dist_esm.annotation.config.style.setDefaultToolStyles({
    global: {
      ...defaultStyles.global,
      ...annotationStyle
    }
  });
}
const toolNames = {
  Pan: dist_esm.PanTool.toolName,
  ArrowAnnotate: dist_esm.ArrowAnnotateTool.toolName,
  WindowLevel: dist_esm.WindowLevelTool.toolName,
  StackScroll: dist_esm.StackScrollTool.toolName,
  StackScrollMouseWheel: dist_esm.StackScrollMouseWheelTool.toolName,
  Zoom: dist_esm.ZoomTool.toolName,
  VolumeRotateMouseWheel: dist_esm.VolumeRotateMouseWheelTool.toolName,
  MipJumpToClick: dist_esm.MIPJumpToClickTool.toolName,
  Length: dist_esm.LengthTool.toolName,
  DragProbe: dist_esm.DragProbeTool.toolName,
  Probe: dist_esm.ProbeTool.toolName,
  RectangleROI: dist_esm.RectangleROITool.toolName,
  EllipticalROI: dist_esm.EllipticalROITool.toolName,
  CircleROI: dist_esm.CircleROITool.toolName,
  Bidirectional: dist_esm.BidirectionalTool.toolName,
  Angle: dist_esm.AngleTool.toolName,
  CobbAngle: dist_esm.CobbAngleTool.toolName,
  PlanarFreehandROI: dist_esm.PlanarFreehandROITool.toolName,
  Magnify: dist_esm.MagnifyTool.toolName,
  Crosshairs: dist_esm.CrosshairsTool.toolName,
  SegmentationDisplay: dist_esm.SegmentationDisplayTool.toolName,
  ReferenceLines: dist_esm.ReferenceLinesTool.toolName,
  CalibrationLine: tools_CalibrationLineTool.toolName,
  TrackballRotateTool: dist_esm.TrackballRotateTool.toolName,
  CircleScissors: dist_esm.CircleScissorsTool.toolName,
  RectangleScissors: dist_esm.RectangleScissorsTool.toolName,
  SphereScissors: dist_esm.SphereScissorsTool.toolName,
  ImageOverlayViewer: tools_ImageOverlayViewerTool.toolName
};

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/constants/supportedTools.js
/* harmony default export */ const supportedTools = (['Length', 'EllipticalROI', 'CircleROI', 'Bidirectional', 'ArrowAnnotate', 'Angle', 'CobbAngle', 'Probe', 'RectangleROI', 'PlanarFreehandROI']);
// EXTERNAL MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/utils/getSOPInstanceAttributes.js
var getSOPInstanceAttributes = __webpack_require__(87172);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/Length.ts



const Length = {
  toAnnotation: measurement => {},
  /**
   * Maps cornerstone annotation event data to measurement service format.
   *
   * @param {Object} cornerstone Cornerstone event data
   * @return {Measurement} Measurement instance
   */
  toMeasurement: (csToolsEventDetail, displaySetService, cornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Length tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, cornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = getMappedAnnotations(annotation, displaySetService);
    const displayText = getDisplayText(mappedAnnotations, displaySet);
    const getReport = () => _getReport(mappedAnnotations, points, FrameOfReferenceUID);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      frameNumber: mappedAnnotations[0]?.frameNumber || 1,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.label,
      displayText: displayText,
      data: data.cachedStats,
      type: getValueTypeFromToolType(toolName),
      getReport
    };
  }
};
function getMappedAnnotations(annotation, displaySetService) {
  const {
    metadata,
    data
  } = annotation;
  const {
    cachedStats
  } = data;
  const {
    referencedImageId
  } = metadata;
  const targets = Object.keys(cachedStats);
  if (!targets.length) {
    return [];
  }
  const annotations = [];
  Object.keys(cachedStats).forEach(targetId => {
    const targetStats = cachedStats[targetId];
    if (!referencedImageId) {
      throw new Error('Non-acquisition plane measurement mapping not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      frameNumber
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId);
    const displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID, frameNumber);
    const {
      SeriesNumber
    } = displaySet;
    const {
      length,
      unit = 'mm'
    } = targetStats;
    annotations.push({
      SeriesInstanceUID,
      SOPInstanceUID,
      SeriesNumber,
      frameNumber,
      unit,
      length
    });
  });
  return annotations;
}

/*
This function is used to convert the measurement data to a format that is
suitable for the report generation (e.g. for the csv report). The report
returns a list of columns and corresponding values.
*/
function _getReport(mappedAnnotations, points, FrameOfReferenceUID) {
  const columns = [];
  const values = [];

  // Add Type
  columns.push('AnnotationType');
  values.push('Cornerstone:Length');
  mappedAnnotations.forEach(annotation => {
    const {
      length,
      unit
    } = annotation;
    columns.push(`Length`);
    values.push(length);
    columns.push('Unit');
    values.push(unit);
  });
  if (FrameOfReferenceUID) {
    columns.push('FrameOfReferenceUID');
    values.push(FrameOfReferenceUID);
  }
  if (points) {
    columns.push('points');
    // points has the form of [[x1, y1, z1], [x2, y2, z2], ...]
    // convert it to string of [[x1 y1 z1];[x2 y2 z2];...]
    // so that it can be used in the csv report
    values.push(points.map(p => p.join(' ')).join(';'));
  }
  return {
    columns,
    values
  };
}
function getDisplayText(mappedAnnotations, displaySet) {
  if (!mappedAnnotations || !mappedAnnotations.length) {
    return '';
  }
  const displayText = [];

  // Area is the same for all series
  const {
    length,
    SeriesNumber,
    SOPInstanceUID,
    frameNumber,
    unit
  } = mappedAnnotations[0];
  const instance = displaySet.images.find(image => image.SOPInstanceUID === SOPInstanceUID);
  let InstanceNumber;
  if (instance) {
    InstanceNumber = instance.InstanceNumber;
  }
  const instanceText = InstanceNumber ? ` I: ${InstanceNumber}` : '';
  const frameText = displaySet.isMultiFrame ? ` F: ${frameNumber}` : '';
  if (length === null || length === undefined) {
    return displayText;
  }
  const roundedLength = src.utils.roundNumber(length, 2);
  displayText.push(`${roundedLength} ${unit} (S: ${SeriesNumber}${instanceText}${frameText})`);
  return displayText;
}
/* harmony default export */ const measurementServiceMappings_Length = (Length);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/utils/getHandlesFromPoints.js
function getHandlesFromPoints(points) {
  if (points.longAxis && points.shortAxis) {
    const handles = {};
    handles.start = points.longAxis[0];
    handles.end = points.longAxis[1];
    handles.perpendicularStart = points.longAxis[0];
    handles.perpendicularEnd = points.longAxis[1];
    return handles;
  }
  return points.map((p, i) => i % 10 === 0 ? {
    start: p
  } : {
    end: p
  }).reduce((obj, item) => Object.assign(obj, {
    ...item
  }), {});
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/utils/selection.ts


/**
 * Check whether an annotation from imaging library is selected or not.
 * @param {string} annotationUID uid of imaging library annotation
 * @returns boolean
 */
function isAnnotationSelected(annotationUID) {
  return dist_esm.annotation.selection.isAnnotationSelected(annotationUID);
}

/**
 * Change an annotation from imaging library's selected property.
 * @param annotationUID - uid of imaging library annotation
 * @param selected - new value for selected
 */
function setAnnotationSelected(annotationUID, selected) {
  const isCurrentSelected = isAnnotationSelected(annotationUID);
  // branch cut, avoid invoking imaging library unnecessarily.
  if (isCurrentSelected !== selected) {
    dist_esm.annotation.selection.setAnnotationSelected(annotationUID, selected);
  }
}
function getFirstAnnotationSelected(element) {
  const [selectedAnnotationUID] = dist_esm.annotation.selection.getAnnotationsSelected() || [];
  if (selectedAnnotationUID) {
    return dist_esm.annotation.state.getAnnotation(selectedAnnotationUID);
  }
}

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/utils/getDisplayUnit.js
const getDisplayUnit = unit => unit == null ? '' : unit;
/* harmony default export */ const utils_getDisplayUnit = (getDisplayUnit);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/utils/index.ts





;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/Bidirectional.ts




const Bidirectional = {
  toAnnotation: measurement => {},
  toMeasurement: (csToolsEventDetail, displaySetService, cornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Length tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, cornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = Bidirectional_getMappedAnnotations(annotation, displaySetService);
    const displayText = Bidirectional_getDisplayText(mappedAnnotations, displaySet);
    const getReport = () => Bidirectional_getReport(mappedAnnotations, points, FrameOfReferenceUID);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      frameNumber: mappedAnnotations[0]?.frameNumber || 1,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.label,
      displayText: displayText,
      data: data.cachedStats,
      type: getValueTypeFromToolType(toolName),
      getReport
    };
  }
};
function Bidirectional_getMappedAnnotations(annotation, displaySetService) {
  const {
    metadata,
    data
  } = annotation;
  const {
    cachedStats
  } = data;
  const {
    referencedImageId,
    referencedSeriesInstanceUID
  } = metadata;
  const targets = Object.keys(cachedStats);
  if (!targets.length) {
    return [];
  }
  const annotations = [];
  Object.keys(cachedStats).forEach(targetId => {
    const targetStats = cachedStats[targetId];
    if (!referencedImageId) {
      throw new Error('Non-acquisition plane measurement mapping not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      frameNumber
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId);
    const displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID, frameNumber);
    const {
      SeriesNumber
    } = displaySet;
    const {
      length,
      width,
      unit
    } = targetStats;
    annotations.push({
      SeriesInstanceUID,
      SOPInstanceUID,
      SeriesNumber,
      frameNumber,
      unit,
      length,
      width
    });
  });
  return annotations;
}

/*
This function is used to convert the measurement data to a format that is
suitable for the report generation (e.g. for the csv report). The report
returns a list of columns and corresponding values.
*/
function Bidirectional_getReport(mappedAnnotations, points, FrameOfReferenceUID) {
  const columns = [];
  const values = [];

  // Add Type
  columns.push('AnnotationType');
  values.push('Cornerstone:Bidirectional');
  mappedAnnotations.forEach(annotation => {
    const {
      length,
      width,
      unit
    } = annotation;
    columns.push(`Length`, `Width`, 'Unit');
    values.push(length, width, unit);
  });
  if (FrameOfReferenceUID) {
    columns.push('FrameOfReferenceUID');
    values.push(FrameOfReferenceUID);
  }
  if (points) {
    columns.push('points');
    // points has the form of [[x1, y1, z1], [x2, y2, z2], ...]
    // convert it to string of [[x1 y1 z1];[x2 y2 z2];...]
    // so that it can be used in the csv report
    values.push(points.map(p => p.join(' ')).join(';'));
  }
  return {
    columns,
    values
  };
}
function Bidirectional_getDisplayText(mappedAnnotations, displaySet) {
  if (!mappedAnnotations || !mappedAnnotations.length) {
    return '';
  }
  const displayText = [];

  // Area is the same for all series
  const {
    length,
    width,
    unit,
    SeriesNumber,
    SOPInstanceUID,
    frameNumber
  } = mappedAnnotations[0];
  const roundedLength = src.utils.roundNumber(length, 2);
  const roundedWidth = src.utils.roundNumber(width, 2);
  const instance = displaySet.images.find(image => image.SOPInstanceUID === SOPInstanceUID);
  let InstanceNumber;
  if (instance) {
    InstanceNumber = instance.InstanceNumber;
  }
  const instanceText = InstanceNumber ? ` I: ${InstanceNumber}` : '';
  const frameText = displaySet.isMultiFrame ? ` F: ${frameNumber}` : '';
  displayText.push(`L: ${roundedLength} ${utils_getDisplayUnit(unit)} (S: ${SeriesNumber}${instanceText}${frameText})`);
  displayText.push(`W: ${roundedWidth} ${utils_getDisplayUnit(unit)}`);
  return displayText;
}
/* harmony default export */ const measurementServiceMappings_Bidirectional = (Bidirectional);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/EllipticalROI.ts




const EllipticalROI = {
  toAnnotation: measurement => {},
  toMeasurement: (csToolsEventDetail, displaySetService, cornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Length tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, cornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = EllipticalROI_getMappedAnnotations(annotation, displaySetService);
    const displayText = EllipticalROI_getDisplayText(mappedAnnotations, displaySet);
    const getReport = () => EllipticalROI_getReport(mappedAnnotations, points, FrameOfReferenceUID);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      frameNumber: mappedAnnotations[0]?.frameNumber || 1,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.label,
      displayText: displayText,
      data: data.cachedStats,
      type: getValueTypeFromToolType(toolName),
      getReport
    };
  }
};
function EllipticalROI_getMappedAnnotations(annotation, displaySetService) {
  const {
    metadata,
    data
  } = annotation;
  const {
    cachedStats
  } = data;
  const {
    referencedImageId
  } = metadata;
  const targets = Object.keys(cachedStats);
  if (!targets.length) {
    return [];
  }
  const annotations = [];
  Object.keys(cachedStats).forEach(targetId => {
    const targetStats = cachedStats[targetId];
    if (!referencedImageId) {
      // Todo: Non-acquisition plane measurement mapping not supported yet
      throw new Error('Non-acquisition plane measurement mapping not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      frameNumber
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId);
    const displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID, frameNumber);
    const {
      SeriesNumber
    } = displaySet;
    const {
      mean,
      stdDev,
      max,
      area,
      Modality,
      areaUnit,
      modalityUnit
    } = targetStats;
    annotations.push({
      SeriesInstanceUID,
      SOPInstanceUID,
      SeriesNumber,
      frameNumber,
      Modality,
      unit: modalityUnit,
      areaUnit,
      mean,
      stdDev,
      max,
      area
    });
  });
  return annotations;
}

/*
This function is used to convert the measurement data to a format that is
suitable for the report generation (e.g. for the csv report). The report
returns a list of columns and corresponding values.
*/
function EllipticalROI_getReport(mappedAnnotations, points, FrameOfReferenceUID) {
  const columns = [];
  const values = [];

  // Add Type
  columns.push('AnnotationType');
  values.push('Cornerstone:EllipticalROI');
  mappedAnnotations.forEach(annotation => {
    const {
      mean,
      stdDev,
      max,
      area,
      unit,
      areaUnit
    } = annotation;
    if (!mean || !unit || !max || !area) {
      return;
    }
    columns.push(`max (${unit})`, `mean (${unit})`, `std (${unit})`, 'Area', 'Unit');
    values.push(max, mean, stdDev, area, areaUnit);
  });
  if (FrameOfReferenceUID) {
    columns.push('FrameOfReferenceUID');
    values.push(FrameOfReferenceUID);
  }
  if (points) {
    columns.push('points');
    // points has the form of [[x1, y1, z1], [x2, y2, z2], ...]
    // convert it to string of [[x1 y1 z1];[x2 y2 z2];...]
    // so that it can be used in the csv report
    values.push(points.map(p => p.join(' ')).join(';'));
  }
  return {
    columns,
    values
  };
}
function EllipticalROI_getDisplayText(mappedAnnotations, displaySet) {
  if (!mappedAnnotations || !mappedAnnotations.length) {
    return '';
  }
  const displayText = [];

  // Area is the same for all series
  const {
    area,
    SOPInstanceUID,
    frameNumber,
    areaUnit
  } = mappedAnnotations[0];
  const instance = displaySet.images.find(image => image.SOPInstanceUID === SOPInstanceUID);
  let InstanceNumber;
  if (instance) {
    InstanceNumber = instance.InstanceNumber;
  }
  const instanceText = InstanceNumber ? ` I: ${InstanceNumber}` : '';
  const frameText = displaySet.isMultiFrame ? ` F: ${frameNumber}` : '';
  const roundedArea = src.utils.roundNumber(area, 2);
  displayText.push(`${roundedArea} ${utils_getDisplayUnit(areaUnit)}`);

  // Todo: we need a better UI for displaying all these information
  mappedAnnotations.forEach(mappedAnnotation => {
    const {
      unit,
      max,
      SeriesNumber
    } = mappedAnnotation;
    let maxStr = '';
    if (max) {
      const roundedMax = src.utils.roundNumber(max, 2);
      maxStr = `Max: ${roundedMax} <small>${utils_getDisplayUnit(unit)}</small> `;
    }
    const str = `${maxStr}(S:${SeriesNumber}${instanceText}${frameText})`;
    if (!displayText.includes(str)) {
      displayText.push(str);
    }
  });
  return displayText;
}
/* harmony default export */ const measurementServiceMappings_EllipticalROI = (EllipticalROI);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/CircleROI.ts




const CircleROI = {
  toAnnotation: measurement => {},
  toMeasurement: (csToolsEventDetail, DisplaySetService, CornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Length tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, CornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = DisplaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = DisplaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = CircleROI_getMappedAnnotations(annotation, DisplaySetService);
    const displayText = CircleROI_getDisplayText(mappedAnnotations, displaySet);
    const getReport = () => CircleROI_getReport(mappedAnnotations, points, FrameOfReferenceUID);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      frameNumber: mappedAnnotations[0]?.frameNumber || 1,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.label,
      displayText: displayText,
      data: data.cachedStats,
      type: getValueTypeFromToolType(toolName),
      getReport
    };
  }
};
function CircleROI_getMappedAnnotations(annotation, DisplaySetService) {
  const {
    metadata,
    data
  } = annotation;
  const {
    cachedStats
  } = data;
  const {
    referencedImageId
  } = metadata;
  const targets = Object.keys(cachedStats);
  if (!targets.length) {
    return [];
  }
  const annotations = [];
  Object.keys(cachedStats).forEach(targetId => {
    const targetStats = cachedStats[targetId];
    if (!referencedImageId) {
      // Todo: Non-acquisition plane measurement mapping not supported yet
      throw new Error('Non-acquisition plane measurement mapping not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      frameNumber
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId);
    const displaySet = DisplaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID, frameNumber);
    const {
      SeriesNumber
    } = displaySet;
    const {
      mean,
      stdDev,
      max,
      area,
      Modality,
      areaUnit,
      modalityUnit
    } = targetStats;
    annotations.push({
      SeriesInstanceUID,
      SOPInstanceUID,
      SeriesNumber,
      frameNumber,
      Modality,
      unit: modalityUnit,
      mean,
      stdDev,
      max,
      area,
      areaUnit
    });
  });
  return annotations;
}

/*
This function is used to convert the measurement data to a format that is
suitable for the report generation (e.g. for the csv report). The report
returns a list of columns and corresponding values.
*/
function CircleROI_getReport(mappedAnnotations, points, FrameOfReferenceUID) {
  const columns = [];
  const values = [];

  // Add Type
  columns.push('AnnotationType');
  values.push('Cornerstone:CircleROI');
  mappedAnnotations.forEach(annotation => {
    const {
      mean,
      stdDev,
      max,
      area,
      unit,
      areaUnit
    } = annotation;
    if (!mean || !unit || !max || !area) {
      return;
    }
    columns.push(`max (${unit})`, `mean (${unit})`, `std (${unit})`, 'Area', 'Unit');
    values.push(max, mean, stdDev, area, areaUnit);
  });
  if (FrameOfReferenceUID) {
    columns.push('FrameOfReferenceUID');
    values.push(FrameOfReferenceUID);
  }
  if (points) {
    columns.push('points');
    // points has the form of [[x1, y1, z1], [x2, y2, z2], ...]
    // convert it to string of [[x1 y1 z1];[x2 y2 z2];...]
    // so that it can be used in the csv report
    values.push(points.map(p => p.join(' ')).join(';'));
  }
  return {
    columns,
    values
  };
}
function CircleROI_getDisplayText(mappedAnnotations, displaySet) {
  if (!mappedAnnotations || !mappedAnnotations.length) {
    return '';
  }
  const displayText = [];

  // Area is the same for all series
  const {
    area,
    SOPInstanceUID,
    frameNumber,
    areaUnit
  } = mappedAnnotations[0];
  const instance = displaySet.images.find(image => image.SOPInstanceUID === SOPInstanceUID);
  let InstanceNumber;
  if (instance) {
    InstanceNumber = instance.InstanceNumber;
  }
  const instanceText = InstanceNumber ? ` I: ${InstanceNumber}` : '';
  const frameText = displaySet.isMultiFrame ? ` F: ${frameNumber}` : '';

  // Area sometimes becomes undefined if `preventHandleOutsideImage` is off.
  const roundedArea = src.utils.roundNumber(area || 0, 2);
  displayText.push(`${roundedArea} ${utils_getDisplayUnit(areaUnit)}`);

  // Todo: we need a better UI for displaying all these information
  mappedAnnotations.forEach(mappedAnnotation => {
    const {
      unit,
      max,
      SeriesNumber
    } = mappedAnnotation;
    let maxStr = '';
    if (max) {
      const roundedMax = src.utils.roundNumber(max, 2);
      maxStr = `Max: ${roundedMax} <small>${utils_getDisplayUnit(unit)}</small> `;
    }
    const str = `${maxStr}(S:${SeriesNumber}${instanceText}${frameText})`;
    if (!displayText.includes(str)) {
      displayText.push(str);
    }
  });
  return displayText;
}
/* harmony default export */ const measurementServiceMappings_CircleROI = (CircleROI);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/ArrowAnnotate.ts


const ArrowAnnotate_Length = {
  toAnnotation: measurement => {},
  /**
   * Maps cornerstone annotation event data to measurement service format.
   *
   * @param {Object} cornerstone Cornerstone event data
   * @return {Measurement} Measurement instance
   */
  toMeasurement: (csToolsEventDetail, displaySetService, cornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Length tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, cornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = ArrowAnnotate_getMappedAnnotations(annotation, displaySetService);
    const displayText = ArrowAnnotate_getDisplayText(mappedAnnotations, displaySet);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      frameNumber: mappedAnnotations[0]?.frameNumber || 1,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.text,
      text: data.text,
      displayText: displayText,
      data: data.cachedStats,
      type: getValueTypeFromToolType(toolName),
      getReport: () => {
        throw new Error('Not implemented');
      }
    };
  }
};
function ArrowAnnotate_getMappedAnnotations(annotation, displaySetService) {
  const {
    metadata,
    data
  } = annotation;
  const {
    text
  } = data;
  const {
    referencedImageId
  } = metadata;
  const annotations = [];
  const {
    SOPInstanceUID,
    SeriesInstanceUID,
    frameNumber
  } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId);
  const displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID, frameNumber);
  const {
    SeriesNumber
  } = displaySet;
  annotations.push({
    SeriesInstanceUID,
    SOPInstanceUID,
    SeriesNumber,
    frameNumber,
    text
  });
  return annotations;
}
function ArrowAnnotate_getDisplayText(mappedAnnotations, displaySet) {
  if (!mappedAnnotations) {
    return '';
  }
  const displayText = [];

  // Area is the same for all series
  const {
    SeriesNumber,
    SOPInstanceUID,
    frameNumber
  } = mappedAnnotations[0];
  const instance = displaySet.images.find(image => image.SOPInstanceUID === SOPInstanceUID);
  let InstanceNumber;
  if (instance) {
    InstanceNumber = instance.InstanceNumber;
  }
  const instanceText = InstanceNumber ? ` I: ${InstanceNumber}` : '';
  const frameText = displaySet.isMultiFrame ? ` F: ${frameNumber}` : '';
  displayText.push(`(S: ${SeriesNumber}${instanceText}${frameText})`);
  return displayText;
}
/* harmony default export */ const ArrowAnnotate = (ArrowAnnotate_Length);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/CobbAngle.ts




const CobbAngle = {
  toAnnotation: measurement => {},
  /**
   * Maps cornerstone annotation event data to measurement service format.
   *
   * @param {Object} cornerstone Cornerstone event data
   * @return {Measurement} Measurement instance
   */
  toMeasurement: (csToolsEventDetail, displaySetService, CornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Cobb Angle tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, CornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = CobbAngle_getMappedAnnotations(annotation, displaySetService);
    const displayText = CobbAngle_getDisplayText(mappedAnnotations, displaySet);
    const getReport = () => CobbAngle_getReport(mappedAnnotations, points, FrameOfReferenceUID);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      frameNumber: mappedAnnotations?.[0]?.frameNumber || 1,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.label,
      displayText: displayText,
      data: data.cachedStats,
      type: getValueTypeFromToolType(toolName),
      getReport
    };
  }
};
function CobbAngle_getMappedAnnotations(annotation, DisplaySetService) {
  const {
    metadata,
    data
  } = annotation;
  const {
    cachedStats
  } = data;
  const {
    referencedImageId
  } = metadata;
  const targets = Object.keys(cachedStats);
  if (!targets.length) {
    return;
  }
  const annotations = [];
  Object.keys(cachedStats).forEach(targetId => {
    const targetStats = cachedStats[targetId];
    if (!referencedImageId) {
      throw new Error('Non-acquisition plane measurement mapping not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      frameNumber
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId);
    const displaySet = DisplaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID, frameNumber);
    const {
      SeriesNumber
    } = displaySet;
    const {
      angle
    } = targetStats;
    const unit = '\u00B0';
    annotations.push({
      SeriesInstanceUID,
      SOPInstanceUID,
      SeriesNumber,
      frameNumber,
      unit,
      angle
    });
  });
  return annotations;
}

/*
This function is used to convert the measurement data to a format that is
suitable for the report generation (e.g. for the csv report). The report
returns a list of columns and corresponding values.
*/
function CobbAngle_getReport(mappedAnnotations, points, FrameOfReferenceUID) {
  const columns = [];
  const values = [];

  // Add Type
  columns.push('AnnotationType');
  values.push('Cornerstone:CobbAngle');
  mappedAnnotations.forEach(annotation => {
    const {
      angle,
      unit
    } = annotation;
    columns.push(`Angle (${unit})`);
    values.push(angle);
  });
  if (FrameOfReferenceUID) {
    columns.push('FrameOfReferenceUID');
    values.push(FrameOfReferenceUID);
  }
  if (points) {
    columns.push('points');
    // points has the form of [[x1, y1, z1], [x2, y2, z2], ...]
    // convert it to string of [[x1 y1 z1];[x2 y2 z2];...]
    // so that it can be used in the csv report
    values.push(points.map(p => p.join(' ')).join(';'));
  }
  return {
    columns,
    values
  };
}
function CobbAngle_getDisplayText(mappedAnnotations, displaySet) {
  if (!mappedAnnotations || !mappedAnnotations.length) {
    return '';
  }
  const displayText = [];

  // Area is the same for all series
  const {
    angle,
    unit,
    SeriesNumber,
    SOPInstanceUID,
    frameNumber
  } = mappedAnnotations[0];
  const instance = displaySet.images.find(image => image.SOPInstanceUID === SOPInstanceUID);
  let InstanceNumber;
  if (instance) {
    InstanceNumber = instance.InstanceNumber;
  }
  const instanceText = InstanceNumber ? ` I: ${InstanceNumber}` : '';
  const frameText = displaySet.isMultiFrame ? ` F: ${frameNumber}` : '';
  if (angle === undefined) {
    return displayText;
  }
  const roundedAngle = src.utils.roundNumber(angle, 2);
  displayText.push(`${roundedAngle} ${utils_getDisplayUnit(unit)} (S: ${SeriesNumber}${instanceText}${frameText})`);
  return displayText;
}
/* harmony default export */ const measurementServiceMappings_CobbAngle = (CobbAngle);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/Angle.ts




const Angle = {
  toAnnotation: measurement => {},
  /**
   * Maps cornerstone annotation event data to measurement service format.
   *
   * @param {Object} cornerstone Cornerstone event data
   * @return {Measurement} Measurement instance
   */
  toMeasurement: (csToolsEventDetail, displaySetService, CornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Length tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, CornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = Angle_getMappedAnnotations(annotation, displaySetService);
    const displayText = Angle_getDisplayText(mappedAnnotations, displaySet);
    const getReport = () => Angle_getReport(mappedAnnotations, points, FrameOfReferenceUID);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      frameNumber: mappedAnnotations?.[0]?.frameNumber || 1,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.label,
      displayText: displayText,
      data: data.cachedStats,
      type: getValueTypeFromToolType(toolName),
      getReport
    };
  }
};
function Angle_getMappedAnnotations(annotation, DisplaySetService) {
  const {
    metadata,
    data
  } = annotation;
  const {
    cachedStats
  } = data;
  const {
    referencedImageId
  } = metadata;
  const targets = Object.keys(cachedStats);
  if (!targets.length) {
    return;
  }
  const annotations = [];
  Object.keys(cachedStats).forEach(targetId => {
    const targetStats = cachedStats[targetId];
    if (!referencedImageId) {
      throw new Error('Non-acquisition plane measurement mapping not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      frameNumber
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId);
    const displaySet = DisplaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID, frameNumber);
    const {
      SeriesNumber
    } = displaySet;
    const {
      angle
    } = targetStats;
    const unit = '\u00B0';
    annotations.push({
      SeriesInstanceUID,
      SOPInstanceUID,
      SeriesNumber,
      frameNumber,
      unit,
      angle
    });
  });
  return annotations;
}

/*
This function is used to convert the measurement data to a format that is
suitable for the report generation (e.g. for the csv report). The report
returns a list of columns and corresponding values.
*/
function Angle_getReport(mappedAnnotations, points, FrameOfReferenceUID) {
  const columns = [];
  const values = [];

  // Add Type
  columns.push('AnnotationType');
  values.push('Cornerstone:Angle');
  mappedAnnotations.forEach(annotation => {
    const {
      angle,
      unit
    } = annotation;
    columns.push(`Angle (${unit})`);
    values.push(angle);
  });
  if (FrameOfReferenceUID) {
    columns.push('FrameOfReferenceUID');
    values.push(FrameOfReferenceUID);
  }
  if (points) {
    columns.push('points');
    // points has the form of [[x1, y1, z1], [x2, y2, z2], ...]
    // convert it to string of [[x1 y1 z1];[x2 y2 z2];...]
    // so that it can be used in the csv report
    values.push(points.map(p => p.join(' ')).join(';'));
  }
  return {
    columns,
    values
  };
}
function Angle_getDisplayText(mappedAnnotations, displaySet) {
  if (!mappedAnnotations || !mappedAnnotations.length) {
    return '';
  }
  const displayText = [];

  // Area is the same for all series
  const {
    angle,
    unit,
    SeriesNumber,
    SOPInstanceUID,
    frameNumber
  } = mappedAnnotations[0];
  const instance = displaySet.images.find(image => image.SOPInstanceUID === SOPInstanceUID);
  let InstanceNumber;
  if (instance) {
    InstanceNumber = instance.InstanceNumber;
  }
  const instanceText = InstanceNumber ? ` I: ${InstanceNumber}` : '';
  const frameText = displaySet.isMultiFrame ? ` F: ${frameNumber}` : '';
  if (angle === undefined) {
    return displayText;
  }
  const roundedAngle = src.utils.roundNumber(angle, 2);
  displayText.push(`${roundedAngle} ${utils_getDisplayUnit(unit)} (S: ${SeriesNumber}${instanceText}${frameText})`);
  return displayText;
}
/* harmony default export */ const measurementServiceMappings_Angle = (Angle);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/PlanarFreehandROI.ts


const PlanarFreehandROI = {
  toAnnotation: measurement => {},
  /**
   * Maps cornerstone annotation event data to measurement service format.
   *
   * @param {Object} cornerstone Cornerstone event data
   * @return {Measurement} Measurement instance
   */
  toMeasurement: (csToolsEventDetail, DisplaySetService, CornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('PlanarFreehandROI tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, CornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = DisplaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = DisplaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = PlanarFreehandROI_getMappedAnnotations(annotation, DisplaySetService);
    const displayText = PlanarFreehandROI_getDisplayText(mappedAnnotations);
    const getReport = () => PlanarFreehandROI_getReport(mappedAnnotations, points, FrameOfReferenceUID);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.label,
      displayText: displayText,
      data: {
        ...data,
        ...data.cachedStats
      },
      type: getValueTypeFromToolType(toolName),
      getReport
    };
  }
};

/**
 * It maps an imaging library annotation to a list of simplified annotation properties.
 *
 * @param {Object} annotationData
 * @param {Object} DisplaySetService
 * @returns
 */
function PlanarFreehandROI_getMappedAnnotations(annotationData, DisplaySetService) {
  const {
    metadata,
    data
  } = annotationData;
  const {
    label
  } = data;
  const {
    referencedImageId
  } = metadata;
  const annotations = [];
  const {
    SOPInstanceUID: _SOPInstanceUID,
    SeriesInstanceUID: _SeriesInstanceUID
  } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId) || {};
  if (!_SOPInstanceUID || !_SeriesInstanceUID) {
    return annotations;
  }
  const displaySet = DisplaySetService.getDisplaySetForSOPInstanceUID(_SOPInstanceUID, _SeriesInstanceUID);
  const {
    SeriesNumber,
    SeriesInstanceUID
  } = displaySet;
  annotations.push({
    SeriesInstanceUID,
    SeriesNumber,
    label,
    data
  });
  return annotations;
}

/**
 * TBD
 * This function is used to convert the measurement data to a format that is suitable for the report generation (e.g. for the csv report).
 * The report returns a list of columns and corresponding values.
 * @param {*} mappedAnnotations
 * @param {*} points
 * @param {*} FrameOfReferenceUID
 * @returns Object representing the report's content for this tool.
 */
function PlanarFreehandROI_getReport(mappedAnnotations, points, FrameOfReferenceUID) {
  const columns = [];
  const values = [];
  return {
    columns,
    values
  };
}
function PlanarFreehandROI_getDisplayText(mappedAnnotations) {
  return '';
}
/* harmony default export */ const measurementServiceMappings_PlanarFreehandROI = (PlanarFreehandROI);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/RectangleROI.ts




const RectangleROI = {
  toAnnotation: measurement => {},
  toMeasurement: (csToolsEventDetail, DisplaySetService, CornerstoneViewportService, getValueTypeFromToolType) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Rectangle ROI tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId, CornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = DisplaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = DisplaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      points
    } = data.handles;
    const mappedAnnotations = RectangleROI_getMappedAnnotations(annotation, DisplaySetService);
    const displayText = RectangleROI_getDisplayText(mappedAnnotations, displaySet);
    const getReport = () => RectangleROI_getReport(mappedAnnotations, points, FrameOfReferenceUID);
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      frameNumber: mappedAnnotations[0]?.frameNumber || 1,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: data.label,
      displayText: displayText,
      data: data.cachedStats,
      type: getValueTypeFromToolType(toolName),
      getReport
    };
  }
};
function RectangleROI_getMappedAnnotations(annotation, DisplaySetService) {
  const {
    metadata,
    data
  } = annotation;
  const {
    cachedStats
  } = data;
  const {
    referencedImageId
  } = metadata;
  const targets = Object.keys(cachedStats);
  if (!targets.length) {
    return [];
  }
  const annotations = [];
  Object.keys(cachedStats).forEach(targetId => {
    const targetStats = cachedStats[targetId];
    if (!referencedImageId) {
      // Todo: Non-acquisition plane measurement mapping not supported yet
      throw new Error('Non-acquisition plane measurement mapping not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      frameNumber
    } = (0,getSOPInstanceAttributes/* default */.Z)(referencedImageId);
    const displaySet = DisplaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID, frameNumber);
    const {
      SeriesNumber
    } = displaySet;
    const {
      mean,
      stdDev,
      max,
      area,
      Modality,
      modalityUnit,
      areaUnit
    } = targetStats;
    annotations.push({
      SeriesInstanceUID,
      SOPInstanceUID,
      SeriesNumber,
      frameNumber,
      Modality,
      unit: modalityUnit,
      mean,
      stdDev,
      max,
      area,
      areaUnit
    });
  });
  return annotations;
}

/*
This function is used to convert the measurement data to a format that is
suitable for the report generation (e.g. for the csv report). The report
returns a list of columns and corresponding values.
*/
function RectangleROI_getReport(mappedAnnotations, points, FrameOfReferenceUID) {
  const columns = [];
  const values = [];

  // Add Type
  columns.push('AnnotationType');
  values.push('Cornerstone:RectangleROI');
  mappedAnnotations.forEach(annotation => {
    const {
      mean,
      stdDev,
      max,
      area,
      unit,
      areaUnit
    } = annotation;
    if (!mean || !unit || !max || !area) {
      return;
    }
    columns.push(`Maximum`, `Mean`, `Std Dev`, 'Pixel Unit', `Area`, 'Unit');
    values.push(max, mean, stdDev, unit, area, areaUnit);
  });
  if (FrameOfReferenceUID) {
    columns.push('FrameOfReferenceUID');
    values.push(FrameOfReferenceUID);
  }
  if (points) {
    columns.push('points');
    // points has the form of [[x1, y1, z1], [x2, y2, z2], ...]
    // convert it to string of [[x1 y1 z1];[x2 y2 z2];...]
    // so that it can be used in the csv report
    values.push(points.map(p => p.join(' ')).join(';'));
  }
  return {
    columns,
    values
  };
}
function RectangleROI_getDisplayText(mappedAnnotations, displaySet) {
  if (!mappedAnnotations || !mappedAnnotations.length) {
    return '';
  }
  const displayText = [];

  // Area is the same for all series
  const {
    area,
    SOPInstanceUID,
    frameNumber,
    areaUnit
  } = mappedAnnotations[0];
  const instance = displaySet.images.find(image => image.SOPInstanceUID === SOPInstanceUID);
  let InstanceNumber;
  if (instance) {
    InstanceNumber = instance.InstanceNumber;
  }
  const instanceText = InstanceNumber ? ` I: ${InstanceNumber}` : '';
  const frameText = displaySet.isMultiFrame ? ` F: ${frameNumber}` : '';

  // Area sometimes becomes undefined if `preventHandleOutsideImage` is off.
  const roundedArea = src.utils.roundNumber(area || 0, 2);
  displayText.push(`${roundedArea} ${utils_getDisplayUnit(areaUnit)}`);

  // Todo: we need a better UI for displaying all these information
  mappedAnnotations.forEach(mappedAnnotation => {
    const {
      unit,
      max,
      SeriesNumber
    } = mappedAnnotation;
    let maxStr = '';
    if (max) {
      const roundedMax = src.utils.roundNumber(max, 2);
      maxStr = `Max: ${roundedMax} <small>${utils_getDisplayUnit(unit)}</small> `;
    }
    const str = `${maxStr}(S:${SeriesNumber}${instanceText}${frameText})`;
    if (!displayText.includes(str)) {
      displayText.push(str);
    }
  });
  return displayText;
}
/* harmony default export */ const measurementServiceMappings_RectangleROI = (RectangleROI);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/measurementServiceMappingsFactory.ts










const measurementServiceMappingsFactory = (measurementService, displaySetService, cornerstoneViewportService) => {
  /**
   * Maps measurement service format object to cornerstone annotation object.
   *
   * @param measurement The measurement instance
   * @param definition The source definition
   * @return Cornerstone annotation data
   */

  const _getValueTypeFromToolType = toolType => {
    const {
      POLYLINE,
      ELLIPSE,
      CIRCLE,
      RECTANGLE,
      BIDIRECTIONAL,
      POINT,
      ANGLE
    } = src.MeasurementService.VALUE_TYPES;

    // TODO -> I get why this was attempted, but its not nearly flexible enough.
    // A single measurement may have an ellipse + a bidirectional measurement, for instances.
    // You can't define a bidirectional tool as a single type..
    const TOOL_TYPE_TO_VALUE_TYPE = {
      Length: POLYLINE,
      EllipticalROI: ELLIPSE,
      CircleROI: CIRCLE,
      RectangleROI: RECTANGLE,
      PlanarFreehandROI: POLYLINE,
      Bidirectional: BIDIRECTIONAL,
      ArrowAnnotate: POINT,
      CobbAngle: ANGLE,
      Angle: ANGLE
    };
    return TOOL_TYPE_TO_VALUE_TYPE[toolType];
  };
  const factories = {
    Length: {
      toAnnotation: measurementServiceMappings_Length.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_Length.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [{
        valueType: src.MeasurementService.VALUE_TYPES.POLYLINE,
        points: 2
      }]
    },
    Bidirectional: {
      toAnnotation: measurementServiceMappings_Bidirectional.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_Bidirectional.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [
      // TODO -> We should eventually do something like shortAxis + longAxis,
      // But its still a little unclear how these automatic interpretations will work.
      {
        valueType: src.MeasurementService.VALUE_TYPES.POLYLINE,
        points: 2
      }, {
        valueType: src.MeasurementService.VALUE_TYPES.POLYLINE,
        points: 2
      }]
    },
    EllipticalROI: {
      toAnnotation: measurementServiceMappings_EllipticalROI.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_EllipticalROI.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [{
        valueType: src.MeasurementService.VALUE_TYPES.ELLIPSE
      }]
    },
    CircleROI: {
      toAnnotation: measurementServiceMappings_CircleROI.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_CircleROI.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [{
        valueType: src.MeasurementService.VALUE_TYPES.CIRCLE
      }]
    },
    RectangleROI: {
      toAnnotation: measurementServiceMappings_RectangleROI.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_RectangleROI.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [{
        valueType: src.MeasurementService.VALUE_TYPES.POLYLINE
      }]
    },
    PlanarFreehandROI: {
      toAnnotation: measurementServiceMappings_PlanarFreehandROI.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_PlanarFreehandROI.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [{
        valueType: src.MeasurementService.VALUE_TYPES.POLYLINE
      }]
    },
    ArrowAnnotate: {
      toAnnotation: ArrowAnnotate.toAnnotation,
      toMeasurement: csToolsAnnotation => ArrowAnnotate.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [{
        valueType: src.MeasurementService.VALUE_TYPES.POINT,
        points: 1
      }]
    },
    CobbAngle: {
      toAnnotation: measurementServiceMappings_CobbAngle.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_CobbAngle.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [{
        valueType: src.MeasurementService.VALUE_TYPES.ANGLE
      }]
    },
    Angle: {
      toAnnotation: measurementServiceMappings_Angle.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_Angle.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService, _getValueTypeFromToolType),
      matchingCriteria: [{
        valueType: src.MeasurementService.VALUE_TYPES.ANGLE
      }]
    }
  };
  return factories;
};
/* harmony default export */ const measurementServiceMappings_measurementServiceMappingsFactory = (measurementServiceMappingsFactory);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/initMeasurementService.js







const {
  removeAnnotation
} = dist_esm.annotation.state;
const csToolsEvents = dist_esm.Enums.Events;
const CORNERSTONE_3D_TOOLS_SOURCE_NAME = 'Cornerstone3DTools';
const CORNERSTONE_3D_TOOLS_SOURCE_VERSION = '0.1';
const initMeasurementService = (measurementService, displaySetService, cornerstoneViewportService) => {
  /* Initialization */
  const {
    Length,
    Bidirectional,
    EllipticalROI,
    CircleROI,
    ArrowAnnotate,
    Angle,
    CobbAngle,
    RectangleROI,
    PlanarFreehandROI
  } = measurementServiceMappings_measurementServiceMappingsFactory(measurementService, displaySetService, cornerstoneViewportService);
  const csTools3DVer1MeasurementSource = measurementService.createSource(CORNERSTONE_3D_TOOLS_SOURCE_NAME, CORNERSTONE_3D_TOOLS_SOURCE_VERSION);

  /* Mappings */
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'Length', Length.matchingCriteria, Length.toAnnotation, Length.toMeasurement);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'Bidirectional', Bidirectional.matchingCriteria, Bidirectional.toAnnotation, Bidirectional.toMeasurement);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'EllipticalROI', EllipticalROI.matchingCriteria, EllipticalROI.toAnnotation, EllipticalROI.toMeasurement);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'CircleROI', CircleROI.matchingCriteria, CircleROI.toAnnotation, CircleROI.toMeasurement);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'ArrowAnnotate', ArrowAnnotate.matchingCriteria, ArrowAnnotate.toAnnotation, ArrowAnnotate.toMeasurement);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'CobbAngle', CobbAngle.matchingCriteria, CobbAngle.toAnnotation, CobbAngle.toMeasurement);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'Angle', Angle.matchingCriteria, Angle.toAnnotation, Angle.toMeasurement);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'RectangleROI', RectangleROI.matchingCriteria, RectangleROI.toAnnotation, RectangleROI.toMeasurement);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'PlanarFreehandROI', PlanarFreehandROI.matchingCriteria, PlanarFreehandROI.toAnnotation, PlanarFreehandROI.toMeasurement);

  // On the UI side, the Calibration Line tool will work almost the same as the
  // Length tool
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'CalibrationLine', Length.matchingCriteria, Length.toAnnotation, Length.toMeasurement);
  return csTools3DVer1MeasurementSource;
};
const connectToolsToMeasurementService = servicesManager => {
  const {
    measurementService,
    displaySetService,
    cornerstoneViewportService
  } = servicesManager.services;
  const csTools3DVer1MeasurementSource = initMeasurementService(measurementService, displaySetService, cornerstoneViewportService);
  connectMeasurementServiceToTools(measurementService, cornerstoneViewportService, csTools3DVer1MeasurementSource);
  const {
    annotationToMeasurement,
    remove
  } = csTools3DVer1MeasurementSource;

  //
  function addMeasurement(csToolsEvent) {
    try {
      const annotationAddedEventDetail = csToolsEvent.detail;
      const {
        annotation: {
          metadata,
          annotationUID
        }
      } = annotationAddedEventDetail;
      const {
        toolName
      } = metadata;
      if (csToolsEvent.type === completedEvt && toolName === toolNames.CalibrationLine) {
        // show modal to input the measurement (mm)
        onCompletedCalibrationLine(servicesManager, csToolsEvent).then(() => {
          console.log('calibration applied');
        }, () => true).finally(() => {
          // we don't need the calibration line lingering around, remove the
          // annotation from the display
          removeAnnotation(annotationUID);
          removeMeasurement(csToolsEvent);
          // this will ensure redrawing of annotations
          cornerstoneViewportService.resize();
        });
      } else {
        // To force the measurementUID be the same as the annotationUID
        // Todo: this should be changed when a measurement can include multiple annotations
        // in the future
        annotationAddedEventDetail.uid = annotationUID;
        annotationToMeasurement(toolName, annotationAddedEventDetail);
      }
    } catch (error) {
      console.warn('Failed to update measurement:', error);
    }
  }
  function updateMeasurement(csToolsEvent) {
    try {
      const annotationModifiedEventDetail = csToolsEvent.detail;
      const {
        annotation: {
          metadata,
          annotationUID
        }
      } = annotationModifiedEventDetail;

      // If the measurement hasn't been added, don't modify it
      const measurement = measurementService.getMeasurement(annotationUID);
      if (!measurement) {
        return;
      }
      const {
        toolName
      } = metadata;
      annotationModifiedEventDetail.uid = annotationUID;
      // Passing true to indicate this is an update and NOT a annotation (start) completion.
      annotationToMeasurement(toolName, annotationModifiedEventDetail, true);
    } catch (error) {
      console.warn('Failed to update measurement:', error);
    }
  }
  function selectMeasurement(csToolsEvent) {
    try {
      const annotationSelectionEventDetail = csToolsEvent.detail;
      const {
        added: addedSelectedAnnotationUIDs,
        removed: removedSelectedAnnotationUIDs
      } = annotationSelectionEventDetail;
      if (removedSelectedAnnotationUIDs) {
        removedSelectedAnnotationUIDs.forEach(annotationUID => measurementService.setMeasurementSelected(annotationUID, false));
      }
      if (addedSelectedAnnotationUIDs) {
        addedSelectedAnnotationUIDs.forEach(annotationUID => measurementService.setMeasurementSelected(annotationUID, true));
      }
    } catch (error) {
      console.warn('Failed to select and unselect measurements:', error);
    }
  }

  /**
   * When csTools fires a removed event, remove the same measurement
   * from the measurement service
   *
   * @param {*} csToolsEvent
   */
  function removeMeasurement(csToolsEvent) {
    try {
      try {
        const annotationRemovedEventDetail = csToolsEvent.detail;
        const {
          annotation: {
            annotationUID
          }
        } = annotationRemovedEventDetail;
        const measurement = measurementService.getMeasurement(annotationUID);
        if (measurement) {
          console.log('~~ removeEvt', csToolsEvent);
          remove(annotationUID, annotationRemovedEventDetail);
        }
      } catch (error) {
        console.warn('Failed to update measurement:', error);
      }
    } catch (error) {
      console.warn('Failed to remove measurement:', error);
    }
  }

  // on display sets added, check if there are any measurements in measurement service that need to be
  // put into cornerstone tools
  const addedEvt = csToolsEvents.ANNOTATION_ADDED;
  const completedEvt = csToolsEvents.ANNOTATION_COMPLETED;
  const updatedEvt = csToolsEvents.ANNOTATION_MODIFIED;
  const removedEvt = csToolsEvents.ANNOTATION_REMOVED;
  const selectionEvt = csToolsEvents.ANNOTATION_SELECTION_CHANGE;
  esm.eventTarget.addEventListener(addedEvt, addMeasurement);
  esm.eventTarget.addEventListener(completedEvt, addMeasurement);
  esm.eventTarget.addEventListener(updatedEvt, updateMeasurement);
  esm.eventTarget.addEventListener(removedEvt, removeMeasurement);
  esm.eventTarget.addEventListener(selectionEvt, selectMeasurement);
  return csTools3DVer1MeasurementSource;
};
const connectMeasurementServiceToTools = (measurementService, cornerstoneViewportService, measurementSource) => {
  const {
    MEASUREMENT_REMOVED,
    MEASUREMENTS_CLEARED,
    MEASUREMENT_UPDATED,
    RAW_MEASUREMENT_ADDED
  } = measurementService.EVENTS;
  const csTools3DVer1MeasurementSource = measurementService.getSource(CORNERSTONE_3D_TOOLS_SOURCE_NAME, CORNERSTONE_3D_TOOLS_SOURCE_VERSION);
  measurementService.subscribe(MEASUREMENTS_CLEARED, _ref => {
    let {
      measurements
    } = _ref;
    if (!Object.keys(measurements).length) {
      return;
    }
    for (const measurement of Object.values(measurements)) {
      const {
        uid,
        source
      } = measurement;
      if (source.name !== CORNERSTONE_3D_TOOLS_SOURCE_NAME) {
        continue;
      }
      removeAnnotation(uid);
    }
  });
  measurementService.subscribe(MEASUREMENT_UPDATED, _ref2 => {
    let {
      source,
      measurement,
      notYetUpdatedAtSource
    } = _ref2;
    if (source.name !== CORNERSTONE_3D_TOOLS_SOURCE_NAME) {
      return;
    }
    if (notYetUpdatedAtSource === false) {
      // This event was fired by cornerstone telling the measurement service to sync.
      // Already in sync.
      return;
    }
    const {
      uid,
      label
    } = measurement;
    const sourceAnnotation = dist_esm.annotation.state.getAnnotation(uid);
    const {
      data,
      metadata
    } = sourceAnnotation;
    if (!data) {
      return;
    }
    if (data.label !== label) {
      data.label = label;
    }
    if (metadata.toolName === 'ArrowAnnotate') {
      data.text = label;
    }

    // Todo: trigger render for annotation
  });

  measurementService.subscribe(RAW_MEASUREMENT_ADDED, _ref3 => {
    let {
      source,
      measurement,
      data,
      dataSource
    } = _ref3;
    if (source.name !== CORNERSTONE_3D_TOOLS_SOURCE_NAME) {
      return;
    }
    const {
      referenceSeriesUID,
      referenceStudyUID,
      SOPInstanceUID
    } = measurement;
    const instance = src.DicomMetadataStore.getInstance(referenceStudyUID, referenceSeriesUID, SOPInstanceUID);
    let imageId;
    let frameNumber = 1;
    if (measurement?.metadata?.referencedImageId) {
      imageId = measurement.metadata.referencedImageId;
      frameNumber = (0,getSOPInstanceAttributes/* default */.Z)(measurement.metadata.referencedImageId).frameNumber;
    } else {
      imageId = dataSource.getImageIdsForInstance({
        instance
      });
    }
    const annotationManager = dist_esm.annotation.state.getAnnotationManager();
    annotationManager.addAnnotation({
      annotationUID: measurement.uid,
      highlighted: false,
      isLocked: false,
      invalidated: false,
      metadata: {
        toolName: measurement.toolName,
        FrameOfReferenceUID: measurement.FrameOfReferenceUID,
        referencedImageId: imageId
      },
      data: {
        text: data.annotation.data.text,
        handles: {
          ...data.annotation.data.handles
        },
        cachedStats: {
          ...data.annotation.data.cachedStats
        },
        label: data.annotation.data.label,
        frameNumber: frameNumber
      }
    });
  });
  measurementService.subscribe(MEASUREMENT_REMOVED, _ref4 => {
    let {
      source,
      measurement: removedMeasurementId
    } = _ref4;
    if (source?.name && source.name !== CORNERSTONE_3D_TOOLS_SOURCE_NAME) {
      return;
    }
    removeAnnotation(removedMeasurementId);
    const renderingEngine = cornerstoneViewportService.getRenderingEngine();
    // Note: We could do a better job by triggering the render on the
    // viewport itself, but the removeAnnotation does not include that info...
    renderingEngine.render();
  });
};

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/initCineService.ts

function initCineService(cineService) {
  const playClip = (element, playClipOptions) => {
    return dist_esm.utilities.cine.playClip(element, playClipOptions);
  };
  const stopClip = element => {
    return dist_esm.utilities.cine.stopClip(element);
  };
  cineService.setServiceImplementation({
    playClip,
    stopClip
  });
}
/* harmony default export */ const src_initCineService = (initCineService);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/getInterleavedFrames.js
function getInterleavedFrames(imageIds) {
  const minImageIdIndex = 0;
  const maxImageIdIndex = imageIds.length - 1;
  const middleImageIdIndex = Math.floor(imageIds.length / 2);
  let lowerImageIdIndex = middleImageIdIndex;
  let upperImageIdIndex = middleImageIdIndex;

  // Build up an array of images to prefetch, starting with the current image.
  const imageIdsToPrefetch = [{
    imageId: imageIds[middleImageIdIndex],
    imageIdIndex: middleImageIdIndex
  }];
  const prefetchQueuedFilled = {
    currentPositionDownToMinimum: false,
    currentPositionUpToMaximum: false
  };

  // Check if on edges and some criteria is already fulfilled

  if (middleImageIdIndex === minImageIdIndex) {
    prefetchQueuedFilled.currentPositionDownToMinimum = true;
  } else if (middleImageIdIndex === maxImageIdIndex) {
    prefetchQueuedFilled.currentPositionUpToMaximum = true;
  }
  while (!prefetchQueuedFilled.currentPositionDownToMinimum || !prefetchQueuedFilled.currentPositionUpToMaximum) {
    if (!prefetchQueuedFilled.currentPositionDownToMinimum) {
      // Add imageId below
      lowerImageIdIndex--;
      imageIdsToPrefetch.push({
        imageId: imageIds[lowerImageIdIndex],
        imageIdIndex: lowerImageIdIndex
      });
      if (lowerImageIdIndex === minImageIdIndex) {
        prefetchQueuedFilled.currentPositionDownToMinimum = true;
      }
    }
    if (!prefetchQueuedFilled.currentPositionUpToMaximum) {
      // Add imageId above
      upperImageIdIndex++;
      imageIdsToPrefetch.push({
        imageId: imageIds[upperImageIdIndex],
        imageIdIndex: upperImageIdIndex
      });
      if (upperImageIdIndex === maxImageIdIndex) {
        prefetchQueuedFilled.currentPositionUpToMaximum = true;
      }
    }
  }
  return imageIdsToPrefetch;
}
// EXTERNAL MODULE: ../../../node_modules/lodash/lodash.js
var lodash = __webpack_require__(44379);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/interleaveCenterLoader.ts




// Map of volumeId and SeriesInstanceId
const volumeIdMapsToLoad = new Map();
const viewportIdVolumeInputArrayMap = new Map();

/**
 * This function caches the volumeUIDs until all the volumes inside the
 * hanging protocol are initialized. Then it goes through the imageIds
 * of the volumes, and interleave them, in order for the volumes to be loaded
 * together from middle to the start and the end.
 * @param {Object} props image loading properties from Cornerstone ViewportService
 * @returns
 */
function interleaveCenterLoader(_ref) {
  let {
    data: {
      viewportId,
      volumeInputArray
    },
    displaySetsMatchDetails,
    viewportMatchDetails: matchDetails
  } = _ref;
  viewportIdVolumeInputArrayMap.set(viewportId, volumeInputArray);

  // Based on the volumeInputs store the volumeIds and SeriesInstanceIds
  // to keep track of the volumes being loaded
  for (const volumeInput of volumeInputArray) {
    const {
      volumeId
    } = volumeInput;
    const volume = esm.cache.getVolume(volumeId);
    if (!volume) {
      return;
    }

    // if the volumeUID is not in the volumeUIDs array, add it
    if (!volumeIdMapsToLoad.has(volumeId)) {
      const {
        metadata
      } = volume;
      volumeIdMapsToLoad.set(volumeId, metadata.SeriesInstanceUID);
    }
  }

  /**
   * The following is checking if all the viewports that were matched in the HP has been
   * successfully created their cornerstone viewport or not. Todo: This can be
   * improved by not checking it, and as soon as the matched DisplaySets have their
   * volume loaded, we start the loading, but that comes at the cost of viewports
   * not being created yet (e.g., in a 10 viewport ptCT fusion, when one ct viewport and one
   * pt viewport are created we have a guarantee that the volumes are created in the cache
   * but the rest of the viewports (fusion, mip etc.) are not created yet. So
   * we can't initiate setting the volumes for those viewports. One solution can be
   * to add an event when a viewport is created (not enabled element event) and then
   * listen to it and as the other viewports are created we can set the volumes for them
   * since volumes are already started loading.
   */
  if (matchDetails.size !== viewportIdVolumeInputArrayMap.size) {
    return;
  }

  // Check if all the matched volumes are loaded
  for (const [_, details] of displaySetsMatchDetails.entries()) {
    const {
      SeriesInstanceUID
    } = details;

    // HangingProtocol has matched, but don't have all the volumes created yet, so return
    if (!Array.from(volumeIdMapsToLoad.values()).includes(SeriesInstanceUID)) {
      return;
    }
  }
  const volumeIds = Array.from(volumeIdMapsToLoad.keys()).slice();
  // get volumes from cache
  const volumes = volumeIds.map(volumeId => {
    return esm.cache.getVolume(volumeId);
  });

  // iterate over all volumes, and get their imageIds, and interleave
  // the imageIds and save them in AllRequests for later use
  const AllRequests = [];
  volumes.forEach(volume => {
    const requests = volume.getImageLoadRequests();
    if (!requests.length || !requests[0] || !requests[0].imageId) {
      return;
    }
    const requestImageIds = requests.map(request => {
      return request.imageId;
    });
    const imageIds = getInterleavedFrames(requestImageIds);
    const reOrderedRequests = imageIds.map(_ref2 => {
      let {
        imageId
      } = _ref2;
      const request = requests.find(req => req.imageId === imageId);
      return request;
    });
    AllRequests.push(reOrderedRequests);
  });

  // flatten the AllRequests array, which will result in a list of all the
  // imageIds for all the volumes but interleaved
  const interleavedRequests = (0,lodash.compact)((0,lodash.flatten)((0,lodash.zip)(...AllRequests)));

  // set the finalRequests to the imageLoadPoolManager
  const finalRequests = [];
  interleavedRequests.forEach(request => {
    const {
      imageId
    } = request;
    AllRequests.forEach(volumeRequests => {
      const volumeImageIdRequest = volumeRequests.find(req => req.imageId === imageId);
      if (volumeImageIdRequest) {
        finalRequests.push(volumeImageIdRequest);
      }
    });
  });
  const requestType = esm.Enums.RequestType.Prefetch;
  const priority = 0;
  finalRequests.forEach(_ref3 => {
    let {
      callLoadImage,
      additionalDetails,
      imageId,
      imageIdIndex,
      options
    } = _ref3;
    const callLoadImageBound = callLoadImage.bind(null, imageId, imageIdIndex, options);
    esm.imageLoadPoolManager.addRequest(callLoadImageBound, requestType, additionalDetails, priority);
  });

  // clear the volumeIdMapsToLoad
  volumeIdMapsToLoad.clear();

  // copy the viewportIdVolumeInputArrayMap
  const viewportIdVolumeInputArrayMapCopy = new Map(viewportIdVolumeInputArrayMap);

  // reset the viewportIdVolumeInputArrayMap
  viewportIdVolumeInputArrayMap.clear();
  return viewportIdVolumeInputArrayMapCopy;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/getNthFrames.js
/**
 * Returns a re-ordered array consisting of, in order:
 *    1. First few objects
 *    2. Center objects
 *    3. Last few objects
 *    4. nth Objects (n=7), set 2
 *    5. nth Objects set 5,
 *    6. Remaining objects
 * What this does is return the first/center/start objects, as those
 * are often used first, then a selection of objects scattered over the
 * instances in order to allow making requests over a set of image instances.
 *
 * @param {[]} imageIds
 * @returns [] reordered to be an nth selection
 */
function getNthFrames(imageIds) {
  const frames = [[], [], [], [], []];
  const centerStart = imageIds.length / 2 - 3;
  const centerEnd = centerStart + 6;
  for (let i = 0; i < imageIds.length; i++) {
    if (i < 2 || i > imageIds.length - 4 || i > centerStart && i < centerEnd) {
      frames[0].push(imageIds[i]);
    } else if (i % 7 === 2) {
      frames[1].push(imageIds[i]);
    } else if (i % 7 === 5) {
      frames[2].push(imageIds[i]);
    } else {
      frames[i % 2 + 3].push(imageIds[i]);
    }
  }
  const ret = [...frames[0], ...frames[1], ...frames[2], ...frames[3], ...frames[4]];
  return ret;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/interleave.js
/**
 * Interleave the items from all the lists so that the first items are first
 * in the returned list, the second items are next etc.
 * Does this in a O(n) fashion, and return lists[0] if there is only one list.
 *
 * @param {[]} lists
 * @returns [] reordered to be breadth first traversal of lists
 */
function interleave(lists) {
  if (!lists || !lists.length) {
    return [];
  }
  if (lists.length === 1) {
    return lists[0];
  }
  console.time('interleave');
  const useLists = [...lists];
  const ret = [];
  for (let i = 0; useLists.length > 0; i++) {
    for (const list of useLists) {
      if (i >= list.length) {
        useLists.splice(useLists.indexOf(list), 1);
        continue;
      }
      ret.push(list[i]);
    }
  }
  console.timeEnd('interleave');
  return ret;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/nthLoader.ts




// Map of volumeId and SeriesInstanceId
const nthLoader_volumeIdMapsToLoad = new Map();
const nthLoader_viewportIdVolumeInputArrayMap = new Map();

/**
 * This function caches the volumeUIDs until all the volumes inside the
 * hanging protocol are initialized. Then it goes through the requests and
 * chooses a sub-selection starting the the first few objects, center objects
 * and last objects, and then the remaining nth images until all instances are
 * retrieved.  This causes the image to have a progressive load order and looks
 * visually much better.
 * @param {Object} props image loading properties from Cornerstone ViewportService
 */
function interleaveNthLoader(_ref) {
  let {
    data: {
      viewportId,
      volumeInputArray
    },
    displaySetsMatchDetails
  } = _ref;
  nthLoader_viewportIdVolumeInputArrayMap.set(viewportId, volumeInputArray);

  // Based on the volumeInputs store the volumeIds and SeriesInstanceIds
  // to keep track of the volumes being loaded
  for (const volumeInput of volumeInputArray) {
    const {
      volumeId
    } = volumeInput;
    const volume = esm.cache.getVolume(volumeId);
    if (!volume) {
      console.log("interleaveNthLoader::No volume, can't load it");
      return;
    }

    // if the volumeUID is not in the volumeUIDs array, add it
    if (!nthLoader_volumeIdMapsToLoad.has(volumeId)) {
      const {
        metadata
      } = volume;
      nthLoader_volumeIdMapsToLoad.set(volumeId, metadata.SeriesInstanceUID);
    }
  }
  const volumeIds = Array.from(nthLoader_volumeIdMapsToLoad.keys()).slice();
  // get volumes from cache
  const volumes = volumeIds.map(volumeId => {
    return esm.cache.getVolume(volumeId);
  });

  // iterate over all volumes, and get their imageIds, and interleave
  // the imageIds and save them in AllRequests for later use
  const originalRequests = volumes.map(volume => volume.getImageLoadRequests()).filter(requests => requests?.[0]?.imageId);
  const orderedRequests = originalRequests.map(request => getNthFrames(request));

  // set the finalRequests to the imageLoadPoolManager
  const finalRequests = interleave(orderedRequests);
  const requestType = esm.Enums.RequestType.Prefetch;
  const priority = 0;
  finalRequests.forEach(_ref2 => {
    let {
      callLoadImage,
      additionalDetails,
      imageId,
      imageIdIndex,
      options
    } = _ref2;
    const callLoadImageBound = callLoadImage.bind(null, imageId, imageIdIndex, options);
    esm.imageLoadPoolManager.addRequest(callLoadImageBound, requestType, additionalDetails, priority);
  });

  // clear the volumeIdMapsToLoad
  nthLoader_volumeIdMapsToLoad.clear();

  // copy the viewportIdVolumeInputArrayMap
  const viewportIdVolumeInputArrayMapCopy = new Map(nthLoader_viewportIdVolumeInputArrayMap);

  // reset the viewportIdVolumeInputArrayMap
  nthLoader_viewportIdVolumeInputArrayMap.clear();
  return viewportIdVolumeInputArrayMapCopy;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/interleaveTopToBottom.ts



// Map of volumeId and SeriesInstanceId
const interleaveTopToBottom_volumeIdMapsToLoad = new Map();
const interleaveTopToBottom_viewportIdVolumeInputArrayMap = new Map();

/**
 * This function caches the volumeIds until all the volumes inside the
 * hanging protocol are initialized. Then it goes through the imageIds
 * of the volumes, and interleave them, in order for the volumes to be loaded
 * together from middle to the start and the end.
 * @param {Object} {viewportData, displaySetMatchDetails}
 * @returns
 */
function interleaveTopToBottom(_ref) {
  let {
    data: {
      viewportId,
      volumeInputArray
    },
    displaySetsMatchDetails,
    viewportMatchDetails: matchDetails
  } = _ref;
  interleaveTopToBottom_viewportIdVolumeInputArrayMap.set(viewportId, volumeInputArray);

  // Based on the volumeInputs store the volumeIds and SeriesInstanceIds
  // to keep track of the volumes being loaded
  for (const volumeInput of volumeInputArray) {
    const {
      volumeId
    } = volumeInput;
    const volume = esm.cache.getVolume(volumeId);
    if (!volume) {
      return;
    }

    // if the volumeUID is not in the volumeUIDs array, add it
    if (!interleaveTopToBottom_volumeIdMapsToLoad.has(volumeId)) {
      const {
        metadata
      } = volume;
      interleaveTopToBottom_volumeIdMapsToLoad.set(volumeId, metadata.SeriesInstanceUID);
    }
  }

  /**
   * The following is checking if all the viewports that were matched in the HP has been
   * successfully created their cornerstone viewport or not. Todo: This can be
   * improved by not checking it, and as soon as the matched DisplaySets have their
   * volume loaded, we start the loading, but that comes at the cost of viewports
   * not being created yet (e.g., in a 10 viewport ptCT fusion, when one ct viewport and one
   * pt viewport are created we have a guarantee that the volumes are created in the cache
   * but the rest of the viewports (fusion, mip etc.) are not created yet. So
   * we can't initiate setting the volumes for those viewports. One solution can be
   * to add an event when a viewport is created (not enabled element event) and then
   * listen to it and as the other viewports are created we can set the volumes for them
   * since volumes are already started loading.
   */
  if (matchDetails.size !== interleaveTopToBottom_viewportIdVolumeInputArrayMap.size) {
    return;
  }

  // Check if all the matched volumes are loaded
  for (const [_, details] of displaySetsMatchDetails.entries()) {
    const {
      SeriesInstanceUID
    } = details;

    // HangingProtocol has matched, but don't have all the volumes created yet, so return
    if (!Array.from(interleaveTopToBottom_volumeIdMapsToLoad.values()).includes(SeriesInstanceUID)) {
      return;
    }
  }
  const volumeIds = Array.from(interleaveTopToBottom_volumeIdMapsToLoad.keys()).slice();
  // get volumes from cache
  const volumes = volumeIds.map(volumeId => {
    return esm.cache.getVolume(volumeId);
  });

  // iterate over all volumes, and get their imageIds, and interleave
  // the imageIds and save them in AllRequests for later use
  const AllRequests = [];
  volumes.forEach(volume => {
    const requests = volume.getImageLoadRequests();
    if (!requests.length || !requests[0] || !requests[0].imageId) {
      return;
    }

    // reverse the requests
    AllRequests.push(requests.reverse());
  });

  // flatten the AllRequests array, which will result in a list of all the
  // imageIds for all the volumes but interleaved
  const interleavedRequests = (0,lodash.compact)((0,lodash.flatten)((0,lodash.zip)(...AllRequests)));

  // set the finalRequests to the imageLoadPoolManager
  const finalRequests = [];
  interleavedRequests.forEach(request => {
    const {
      imageId
    } = request;
    AllRequests.forEach(volumeRequests => {
      const volumeImageIdRequest = volumeRequests.find(req => req.imageId === imageId);
      if (volumeImageIdRequest) {
        finalRequests.push(volumeImageIdRequest);
      }
    });
  });
  const requestType = esm.Enums.RequestType.Prefetch;
  const priority = 0;
  finalRequests.forEach(_ref2 => {
    let {
      callLoadImage,
      additionalDetails,
      imageId,
      imageIdIndex,
      options
    } = _ref2;
    const callLoadImageBound = callLoadImage.bind(null, imageId, imageIdIndex, options);
    esm.imageLoadPoolManager.addRequest(callLoadImageBound, requestType, additionalDetails, priority);
  });

  // clear the volumeIdMapsToLoad
  interleaveTopToBottom_volumeIdMapsToLoad.clear();

  // copy the viewportIdVolumeInputArrayMap
  const viewportIdVolumeInputArrayMapCopy = new Map(interleaveTopToBottom_viewportIdVolumeInputArrayMap);

  // reset the viewportIdVolumeInputArrayMap
  interleaveTopToBottom_viewportIdVolumeInputArrayMap.clear();
  return viewportIdVolumeInputArrayMapCopy;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/findNearbyToolData.ts
/**
 * Finds tool nearby event position triggered.
 *
 * @param {Object} commandsManager mannager of commands
 * @param {Object} event that has being triggered
 * @returns cs toolData or undefined if not found.
 */
const findNearbyToolData = (commandsManager, evt) => {
  if (!evt?.detail) {
    return;
  }
  const {
    element,
    currentPoints
  } = evt.detail;
  return commandsManager.runCommand('getNearbyAnnotation', {
    element,
    canvasCoordinates: currentPoints?.canvas
  }, 'CORNERSTONE');
};
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/initContextMenu.ts




const cs3DToolsEvents = dist_esm.Enums.Events;
const DEFAULT_CONTEXT_MENU_CLICKS = {
  button1: {
    commands: [{
      commandName: 'closeContextMenu'
    }]
  },
  button3: {
    commands: [{
      commandName: 'showCornerstoneContextMenu',
      commandOptions: {
        requireNearbyToolData: true,
        menuId: 'measurementsContextMenu'
      }
    }]
  }
};

/**
 * Generates a name, consisting of:
 *    * alt when the alt key is down
 *    * ctrl when the cctrl key is down
 *    * shift when the shift key is down
 *    * 'button' followed by the button number (1 left, 3 right etc)
 */
function getEventName(evt) {
  const button = evt.detail.event.which;
  const nameArr = [];
  if (evt.detail.event.altKey) {
    nameArr.push('alt');
  }
  if (evt.detail.event.ctrlKey) {
    nameArr.push('ctrl');
  }
  if (evt.detail.event.shiftKey) {
    nameArr.push('shift');
  }
  nameArr.push('button');
  nameArr.push(button);
  return nameArr.join('');
}
function initContextMenu(_ref) {
  let {
    cornerstoneViewportService,
    customizationService,
    commandsManager
  } = _ref;
  /*
   * Run the commands associated with the given button press,
   * defaults on button1 and button2
   */
  const cornerstoneViewportHandleEvent = (name, evt) => {
    const customizations = customizationService.get('cornerstoneViewportClickCommands') || DEFAULT_CONTEXT_MENU_CLICKS;
    const toRun = customizations[name];
    if (!toRun) {
      return;
    }

    // only find nearbyToolData if required, for the click (which closes the context menu
    // we don't need to find nearbyToolData)
    let nearbyToolData = null;
    if (toRun.commands.some(command => command.commandOptions?.requireNearbyToolData)) {
      nearbyToolData = findNearbyToolData(commandsManager, evt);
    }
    const options = {
      nearbyToolData,
      event: evt
    };
    commandsManager.run(toRun, options);
  };
  const cornerstoneViewportHandleClick = evt => {
    const name = getEventName(evt);
    cornerstoneViewportHandleEvent(name, evt);
  };
  function elementEnabledHandler(evt) {
    const {
      viewportId,
      element
    } = evt.detail;
    const viewportInfo = cornerstoneViewportService.getViewportInfo(viewportId);
    if (!viewportInfo) {
      return;
    }
    // TODO check update upstream
    (0,state/* setEnabledElement */.Yc)(viewportId, element);
    element.addEventListener(cs3DToolsEvents.MOUSE_CLICK, cornerstoneViewportHandleClick);
  }
  function elementDisabledHandler(evt) {
    const {
      element
    } = evt.detail;
    element.removeEventListener(cs3DToolsEvents.MOUSE_CLICK, cornerstoneViewportHandleClick);
  }
  esm.eventTarget.addEventListener(esm.EVENTS.ELEMENT_ENABLED, elementEnabledHandler.bind(null));
  esm.eventTarget.addEventListener(esm.EVENTS.ELEMENT_DISABLED, elementDisabledHandler.bind(null));
}
/* harmony default export */ const src_initContextMenu = (initContextMenu);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/initDoubleClick.ts



const initDoubleClick_cs3DToolsEvents = dist_esm.Enums.Events;
const DEFAULT_DOUBLE_CLICK = {
  doubleClick: {
    commandName: 'toggleOneUp',
    commandOptions: {}
  }
};

/**
 * Generates a double click event name, consisting of:
 *    * alt when the alt key is down
 *    * ctrl when the cctrl key is down
 *    * shift when the shift key is down
 *    * 'doubleClick'
 */
function getDoubleClickEventName(evt) {
  const nameArr = [];
  if (evt.detail.event.altKey) {
    nameArr.push('alt');
  }
  if (evt.detail.event.ctrlKey) {
    nameArr.push('ctrl');
  }
  if (evt.detail.event.shiftKey) {
    nameArr.push('shift');
  }
  nameArr.push('doubleClick');
  return nameArr.join('');
}
function initDoubleClick(_ref) {
  let {
    customizationService,
    commandsManager
  } = _ref;
  const cornerstoneViewportHandleDoubleClick = evt => {
    // Do not allow double click on a tool.
    const nearbyToolData = findNearbyToolData(commandsManager, evt);
    if (nearbyToolData) {
      return;
    }
    const eventName = getDoubleClickEventName(evt);

    // Allows for the customization of the double click on a viewport.
    const customizations = customizationService.get('cornerstoneViewportClickCommands') || DEFAULT_DOUBLE_CLICK;
    const toRun = customizations[eventName];
    if (!toRun) {
      return;
    }
    commandsManager.run(toRun);
  };
  function elementEnabledHandler(evt) {
    const {
      element
    } = evt.detail;
    element.addEventListener(initDoubleClick_cs3DToolsEvents.MOUSE_DOUBLE_CLICK, cornerstoneViewportHandleDoubleClick);
  }
  function elementDisabledHandler(evt) {
    const {
      element
    } = evt.detail;
    element.removeEventListener(initDoubleClick_cs3DToolsEvents.MOUSE_DOUBLE_CLICK, cornerstoneViewportHandleDoubleClick);
  }
  esm.eventTarget.addEventListener(esm.EVENTS.ELEMENT_ENABLED, elementEnabledHandler.bind(null));
  esm.eventTarget.addEventListener(esm.EVENTS.ELEMENT_DISABLED, elementDisabledHandler.bind(null));
}
/* harmony default export */ const src_initDoubleClick = (initDoubleClick);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/initViewTiming.ts


const {
  TimingEnum
} = src.Types;
const IMAGE_TIMING_KEYS = [TimingEnum.DISPLAY_SETS_TO_ALL_IMAGES, TimingEnum.DISPLAY_SETS_TO_FIRST_IMAGE, TimingEnum.STUDY_TO_FIRST_IMAGE];
const imageTiming = {
  viewportsWaiting: 0
};

/**
 * Defines the initial view timing reporting.
 * This allows knowing how many viewports are waiting for initial views and
 * when the IMAGE_RENDERED gets sent out.
 * The first image rendered will fire the FIRST_IMAGE timeEnd logs, while
 * the last of the enabled viewport will fire the ALL_IMAGES timeEnd logs.
 *
 */

function initViewTiming(_ref) {
  let {
    element
  } = _ref;
  if (!IMAGE_TIMING_KEYS.find(key => src/* log */.cM.timingKeys[key])) {
    return;
  }
  imageTiming.viewportsWaiting += 1;
  element.addEventListener(esm.EVENTS.IMAGE_RENDERED, imageRenderedListener);
}
function imageRenderedListener(evt) {
  if (evt.detail.viewportStatus === 'preRender') {
    return;
  }
  src/* log */.cM.timeEnd(TimingEnum.DISPLAY_SETS_TO_FIRST_IMAGE);
  src/* log */.cM.timeEnd(TimingEnum.STUDY_TO_FIRST_IMAGE);
  src/* log */.cM.timeEnd(TimingEnum.SCRIPT_TO_VIEW);
  imageTiming.viewportsWaiting -= 1;
  evt.detail.element.removeEventListener(esm.EVENTS.IMAGE_RENDERED, imageRenderedListener);
  if (!imageTiming.viewportsWaiting) {
    src/* log */.cM.timeEnd(TimingEnum.DISPLAY_SETS_TO_ALL_IMAGES);
  }
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/init.tsx


















// TODO: Cypress tests are currently grabbing this from the window?
window.cornerstone = esm;
window.cornerstoneTools = dist_esm;
/**
 *
 */
async function init(_ref) {
  let {
    servicesManager,
    commandsManager,
    extensionManager,
    configuration,
    appConfig
  } = _ref;
  // Note: this should run first before initializing the cornerstone
  // DO NOT CHANGE THE ORDER
  const value = appConfig.useSharedArrayBuffer;
  let sharedArrayBufferDisabled = false;
  if (value === 'AUTO') {
    esm.setUseSharedArrayBuffer(esm.Enums.SharedArrayBufferModes.AUTO);
  } else if (value === 'FALSE' || value === false) {
    esm.setUseSharedArrayBuffer(esm.Enums.SharedArrayBufferModes.FALSE);
    sharedArrayBufferDisabled = true;
  } else {
    esm.setUseSharedArrayBuffer(esm.Enums.SharedArrayBufferModes.TRUE);
  }
  await (0,esm.init)({
    rendering: {
      preferSizeOverAccuracy: Boolean(appConfig.use16BitDataType),
      useNorm16Texture: Boolean(appConfig.use16BitDataType)
    }
  });

  // For debugging e2e tests that are failing on CI
  esm.setUseCPURendering(Boolean(appConfig.useCPURendering));
  esm.setConfiguration({
    ...esm.getConfiguration(),
    rendering: {
      ...esm.getConfiguration().rendering,
      strictZSpacingForVolumeViewport: appConfig.strictZSpacingForVolumeViewport
    }
  });

  // For debugging large datasets, otherwise prefer the defaults
  const {
    maxCacheSize
  } = appConfig;
  if (maxCacheSize) {
    esm.cache.setMaxCacheSize(maxCacheSize);
  }
  initCornerstoneTools();
  esm.Settings.getRuntimeSettings().set('useCursors', Boolean(appConfig.useCursors));
  const {
    userAuthenticationService,
    customizationService,
    uiModalService,
    uiNotificationService,
    cineService,
    cornerstoneViewportService,
    hangingProtocolService,
    toolGroupService,
    toolbarService,
    viewportGridService,
    stateSyncService
  } = servicesManager.services;
  window.services = servicesManager.services;
  window.extensionManager = extensionManager;
  window.commandsManager = commandsManager;
  if (appConfig.showWarningMessageForCrossOrigin && !window.crossOriginIsolated && !sharedArrayBufferDisabled) {
    uiNotificationService.show({
      title: 'Cross Origin Isolation',
      message: 'Cross Origin Isolation is not enabled, read more about it here: https://docs.ohif.org/faq/',
      type: 'warning'
    });
  }
  if (appConfig.showCPUFallbackMessage && esm.getShouldUseCPURendering()) {
    _showCPURenderingModal(uiModalService, hangingProtocolService);
  }

  // Stores a map from `lutPresentationId` to a Presentation object so that
  // an OHIFCornerstoneViewport can be redisplayed with the same LUT
  stateSyncService.register('lutPresentationStore', {
    clearOnModeExit: true
  });

  // Stores a map from `positionPresentationId` to a Presentation object so that
  // an OHIFCornerstoneViewport can be redisplayed with the same position
  stateSyncService.register('positionPresentationStore', {
    clearOnModeExit: true
  });

  // Stores the entire ViewportGridService getState when toggling to one up
  // (e.g. via a double click) so that it can be restored when toggling back.
  stateSyncService.register('toggleOneUpViewportGridStore', {
    clearOnModeExit: true
  });
  const labelmapRepresentation = dist_esm.Enums.SegmentationRepresentations.Labelmap;
  dist_esm.segmentation.config.setGlobalRepresentationConfig(labelmapRepresentation, {
    fillAlpha: 0.3,
    fillAlphaInactive: 0.2,
    outlineOpacity: 1,
    outlineOpacityInactive: 0.65
  });
  const metadataProvider = src["default"].classes.MetadataProvider;
  esm.volumeLoader.registerVolumeLoader('cornerstoneStreamingImageVolume', streaming_image_volume_loader_dist_esm/* cornerstoneStreamingImageVolumeLoader */.IU);
  hangingProtocolService.registerImageLoadStrategy('interleaveCenter', interleaveCenterLoader);
  hangingProtocolService.registerImageLoadStrategy('interleaveTopToBottom', interleaveTopToBottom);
  hangingProtocolService.registerImageLoadStrategy('nth', interleaveNthLoader);

  // add metadata providers
  esm.metaData.addProvider(esm.utilities.calibratedPixelSpacingMetadataProvider.get.bind(esm.utilities.calibratedPixelSpacingMetadataProvider)); // this provider is required for Calibration tool
  esm.metaData.addProvider(metadataProvider.get.bind(metadataProvider), 9999);
  esm.imageLoadPoolManager.maxNumRequests = {
    interaction: appConfig?.maxNumRequests?.interaction || 100,
    thumbnail: appConfig?.maxNumRequests?.thumbnail || 75,
    prefetch: appConfig?.maxNumRequests?.prefetch || 10
  };
  initWADOImageLoader(userAuthenticationService, appConfig, extensionManager);

  /* Measurement Service */
  this.measurementServiceSource = connectToolsToMeasurementService(servicesManager);
  src_initCineService(cineService);

  // When a custom image load is performed, update the relevant viewports
  hangingProtocolService.subscribe(hangingProtocolService.EVENTS.CUSTOM_IMAGE_LOAD_PERFORMED, volumeInputArrayMap => {
    for (const entry of volumeInputArrayMap.entries()) {
      const [viewportId, volumeInputArray] = entry;
      const viewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
      const ohifViewport = cornerstoneViewportService.getViewportInfo(viewportId);
      const {
        lutPresentationStore,
        positionPresentationStore
      } = stateSyncService.getState();
      const {
        presentationIds
      } = ohifViewport.getViewportOptions();
      const presentations = {
        positionPresentation: positionPresentationStore[presentationIds?.positionPresentationId],
        lutPresentation: lutPresentationStore[presentationIds?.lutPresentationId]
      };
      cornerstoneViewportService.setVolumesForViewport(viewport, volumeInputArray, presentations);
    }
  });
  src_initContextMenu({
    cornerstoneViewportService,
    customizationService,
    commandsManager
  });
  src_initDoubleClick({
    customizationService,
    commandsManager
  });

  /**
   * When a viewport gets a new display set, this call will go through all the
   * active tools in the toolbar, and call any commands registered in the
   * toolbar service with a callback to re-enable on displaying the viewport.
   */
  const toolbarEventListener = evt => {
    const {
      element
    } = evt.detail;
    const activeTools = toolbarService.getActiveTools();
    activeTools.forEach(tool => {
      const toolData = toolbarService.getNestedButton(tool);
      const commands = toolData?.listeners?.[evt.type];
      commandsManager.run(commands, {
        element,
        evt
      });
    });
  };

  /** Listens for active viewport events and fires the toolbar listeners */
  const activeViewportEventListener = evt => {
    const {
      viewportId
    } = evt;
    const toolGroup = toolGroupService.getToolGroupForViewport(viewportId);
    const activeTools = toolbarService.getActiveTools();
    activeTools.forEach(tool => {
      if (!toolGroup?._toolInstances?.[tool]) {
        return;
      }

      // check if tool is active on the new viewport
      const toolEnabled = toolGroup._toolInstances[tool].mode === dist_esm.Enums.ToolModes.Enabled;
      if (!toolEnabled) {
        return;
      }
      const button = toolbarService.getNestedButton(tool);
      const commands = button?.listeners?.[evt.type];
      commandsManager.run(commands, {
        viewportId,
        evt
      });
    });
  };
  const resetCrosshairs = evt => {
    const {
      element
    } = evt.detail;
    const {
      viewportId,
      renderingEngineId
    } = esm.getEnabledElement(element);
    const toolGroup = dist_esm.ToolGroupManager.getToolGroupForViewport(viewportId, renderingEngineId);
    if (!toolGroup || !toolGroup._toolInstances?.['Crosshairs']) {
      return;
    }
    const mode = toolGroup._toolInstances['Crosshairs'].mode;
    if (mode === dist_esm.Enums.ToolModes.Active) {
      toolGroup.setToolActive('Crosshairs');
    } else if (mode === dist_esm.Enums.ToolModes.Passive) {
      toolGroup.setToolPassive('Crosshairs');
    } else if (mode === dist_esm.Enums.ToolModes.Enabled) {
      toolGroup.setToolEnabled('Crosshairs');
    }
  };
  esm.eventTarget.addEventListener(esm.EVENTS.STACK_VIEWPORT_NEW_STACK, evt => {
    const {
      element
    } = evt.detail;
    dist_esm.utilities.stackContextPrefetch.enable(element);
  });
  function elementEnabledHandler(evt) {
    const {
      element
    } = evt.detail;
    element.addEventListener(esm.EVENTS.CAMERA_RESET, resetCrosshairs);
    esm.eventTarget.addEventListener(esm.EVENTS.STACK_VIEWPORT_NEW_STACK, toolbarEventListener);
    initViewTiming({
      element,
      eventTarget: esm.eventTarget
    });
  }
  function elementDisabledHandler(evt) {
    const {
      element
    } = evt.detail;
    element.removeEventListener(esm.EVENTS.CAMERA_RESET, resetCrosshairs);

    // TODO - consider removing the callback when all elements are gone
    // eventTarget.removeEventListener(
    //   EVENTS.STACK_VIEWPORT_NEW_STACK,
    //   newStackCallback
    // );
  }

  esm.eventTarget.addEventListener(esm.EVENTS.ELEMENT_ENABLED, elementEnabledHandler.bind(null));
  esm.eventTarget.addEventListener(esm.EVENTS.ELEMENT_DISABLED, elementDisabledHandler.bind(null));
  viewportGridService.subscribe(viewportGridService.EVENTS.ACTIVE_VIEWPORT_ID_CHANGED, activeViewportEventListener);
}
function CPUModal() {
  return /*#__PURE__*/react.createElement("div", null, /*#__PURE__*/react.createElement("p", null, "Your computer does not have enough GPU power to support the default GPU rendering mode. OHIF has switched to CPU rendering mode. Please note that CPU rendering does not support all features such as Volume Rendering, Multiplanar Reconstruction, and Segmentation Overlays."));
}
function _showCPURenderingModal(uiModalService, hangingProtocolService) {
  const callback = progress => {
    if (progress === 100) {
      uiModalService.show({
        content: CPUModal,
        title: 'OHIF Fell Back to CPU Rendering'
      });
      return true;
    }
  };
  const {
    unsubscribe
  } = hangingProtocolService.subscribe(hangingProtocolService.EVENTS.PROTOCOL_CHANGED, () => {
    const done = callback(100);
    if (done) {
      unsubscribe();
    }
  });
}
// EXTERNAL MODULE: ../../../node_modules/react-dropzone/dist/es/index.js + 5 modules
var es = __webpack_require__(74834);
// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../../../node_modules/classnames/index.js
var classnames = __webpack_require__(44921);
var classnames_default = /*#__PURE__*/__webpack_require__.n(classnames);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/DicomFileUploader.ts


const EVENTS = {
  PROGRESS: 'event:DicomFileUploader:progress'
};
let UploadStatus = /*#__PURE__*/function (UploadStatus) {
  UploadStatus[UploadStatus["NotStarted"] = 0] = "NotStarted";
  UploadStatus[UploadStatus["InProgress"] = 1] = "InProgress";
  UploadStatus[UploadStatus["Success"] = 2] = "Success";
  UploadStatus[UploadStatus["Failed"] = 3] = "Failed";
  UploadStatus[UploadStatus["Cancelled"] = 4] = "Cancelled";
  return UploadStatus;
}({});
class UploadRejection {
  constructor(status, message) {
    this.message = void 0;
    this.status = void 0;
    this.message = message;
    this.status = status;
  }
}
class DicomFileUploader extends src/* PubSubService */.hC {
  constructor(file, dataSource) {
    super(EVENTS);
    this._file = void 0;
    this._fileId = void 0;
    this._dataSource = void 0;
    this._loadPromise = void 0;
    this._abortController = new AbortController();
    this._status = UploadStatus.NotStarted;
    this._percentComplete = 0;
    this._file = file;
    this._fileId = cornerstoneDICOMImageLoader_min_default().wadouri.fileManager.add(file);
    this._dataSource = dataSource;
  }
  getFileId() {
    return this._fileId;
  }
  getFileName() {
    return this._file.name;
  }
  getFileSize() {
    return this._file.size;
  }
  cancel() {
    this._abortController.abort();
  }
  getStatus() {
    return this._status;
  }
  getPercentComplete() {
    return this._percentComplete;
  }
  async load() {
    if (this._loadPromise) {
      // Already started loading, return the load promise.
      return this._loadPromise;
    }
    this._loadPromise = new Promise((resolve, reject) => {
      // The upload listeners: fire progress events and/or settle the promise.
      const uploadCallbacks = {
        progress: evt => {
          if (!evt.lengthComputable) {
            // Progress computation is not possible.
            return;
          }
          this._status = UploadStatus.InProgress;
          this._percentComplete = Math.round(100 * evt.loaded / evt.total);
          this._broadcastEvent(EVENTS.PROGRESS, {
            fileId: this._fileId,
            percentComplete: this._percentComplete
          });
        },
        timeout: () => {
          this._reject(reject, new UploadRejection(UploadStatus.Failed, 'The request timed out.'));
        },
        abort: () => {
          this._reject(reject, new UploadRejection(UploadStatus.Cancelled, 'Cancelled'));
        },
        error: () => {
          this._reject(reject, new UploadRejection(UploadStatus.Failed, 'The request failed.'));
        }
      };

      // First try to load the file.
      cornerstoneDICOMImageLoader_min_default().wadouri.loadFileRequest(this._fileId).then(dicomFile => {
        if (this._abortController.signal.aborted) {
          this._reject(reject, new UploadRejection(UploadStatus.Cancelled, 'Cancelled'));
          return;
        }
        if (!this._checkDicomFile(dicomFile)) {
          // The file is not DICOM
          this._reject(reject, new UploadRejection(UploadStatus.Failed, 'Not a valid DICOM file.'));
          return;
        }
        const request = new XMLHttpRequest();
        this._addRequestCallbacks(request, uploadCallbacks);

        // Do the actual upload by supplying the DICOM file and upload callbacks/listeners.
        return this._dataSource.store.dicom(dicomFile, request).then(() => {
          this._status = UploadStatus.Success;
          resolve();
        }).catch(reason => {
          this._reject(reject, reason);
        });
      }).catch(reason => {
        this._reject(reject, reason);
      });
    });
    return this._loadPromise;
  }
  _isRejected() {
    return this._status === UploadStatus.Failed || this._status === UploadStatus.Cancelled;
  }
  _reject(reject, reason) {
    if (this._isRejected()) {
      return;
    }
    if (reason instanceof UploadRejection) {
      this._status = reason.status;
      reject(reason);
      return;
    }
    this._status = UploadStatus.Failed;
    if (reason.message) {
      reject(new UploadRejection(UploadStatus.Failed, reason.message));
      return;
    }
    reject(new UploadRejection(UploadStatus.Failed, reason));
  }
  _addRequestCallbacks(request, uploadCallbacks) {
    const abortCallback = () => request.abort();
    this._abortController.signal.addEventListener('abort', abortCallback);
    for (const [eventName, callback] of Object.entries(uploadCallbacks)) {
      request.upload.addEventListener(eventName, callback);
    }
    const cleanUpCallback = () => {
      this._abortController.signal.removeEventListener('abort', abortCallback);
      for (const [eventName, callback] of Object.entries(uploadCallbacks)) {
        request.upload.removeEventListener(eventName, callback);
      }
      request.removeEventListener('loadend', cleanUpCallback);
    };
    request.addEventListener('loadend', cleanUpCallback);
  }
  _checkDicomFile(arrayBuffer) {
    if (arrayBuffer.length <= 132) {
      return false;
    }
    const arr = new Uint8Array(arrayBuffer.slice(128, 132));
    // bytes from 128 to 132 must be "DICM"
    return Array.from('DICM').every((char, i) => char.charCodeAt(0) === arr[i]);
  }
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/components/DicomUpload/DicomUploadProgressItem.tsx




// eslint-disable-next-line react/display-name
const DicomUploadProgressItem = /*#__PURE__*/(0,react.memo)(_ref => {
  let {
    dicomFileUploader
  } = _ref;
  const [percentComplete, setPercentComplete] = (0,react.useState)(dicomFileUploader.getPercentComplete());
  const [failedReason, setFailedReason] = (0,react.useState)('');
  const [status, setStatus] = (0,react.useState)(dicomFileUploader.getStatus());
  const isComplete = (0,react.useCallback)(() => {
    return status === UploadStatus.Failed || status === UploadStatus.Cancelled || status === UploadStatus.Success;
  }, [status]);
  (0,react.useEffect)(() => {
    const progressSubscription = dicomFileUploader.subscribe(EVENTS.PROGRESS, dicomFileUploaderProgressEvent => {
      setPercentComplete(dicomFileUploaderProgressEvent.percentComplete);
    });
    dicomFileUploader.load().catch(reason => {
      setStatus(reason.status);
      setFailedReason(reason.message ?? '');
    }).finally(() => setStatus(dicomFileUploader.getStatus()));
    return () => progressSubscription.unsubscribe();
  }, []);
  const cancelUpload = (0,react.useCallback)(() => {
    dicomFileUploader.cancel();
  }, []);
  const getStatusIcon = () => {
    switch (dicomFileUploader.getStatus()) {
      case UploadStatus.Success:
        return /*#__PURE__*/react.createElement(ui_src/* Icon */.JO, {
          name: "status-tracked",
          className: "text-primary-light"
        });
      case UploadStatus.InProgress:
        return /*#__PURE__*/react.createElement(ui_src/* Icon */.JO, {
          name: "icon-transferring"
        });
      case UploadStatus.Failed:
        return /*#__PURE__*/react.createElement(ui_src/* Icon */.JO, {
          name: "icon-alert-small"
        });
      case UploadStatus.Cancelled:
        return /*#__PURE__*/react.createElement(ui_src/* Icon */.JO, {
          name: "icon-alert-outline"
        });
      default:
        return /*#__PURE__*/react.createElement(react.Fragment, null);
    }
  };
  return /*#__PURE__*/react.createElement("div", {
    className: "min-h-14 border-secondary-light flex w-full items-center overflow-hidden border-b p-2.5 text-lg"
  }, /*#__PURE__*/react.createElement("div", {
    className: "self-top flex w-0 shrink grow flex-col gap-1"
  }, /*#__PURE__*/react.createElement("div", {
    className: "flex gap-4"
  }, /*#__PURE__*/react.createElement("div", {
    className: "flex w-6 shrink-0 items-center justify-center"
  }, getStatusIcon()), /*#__PURE__*/react.createElement("div", {
    className: "overflow-hidden text-ellipsis whitespace-nowrap"
  }, dicomFileUploader.getFileName())), failedReason && /*#__PURE__*/react.createElement("div", {
    className: "pl-10"
  }, failedReason)), /*#__PURE__*/react.createElement("div", {
    className: "flex w-24 items-center"
  }, !isComplete() && /*#__PURE__*/react.createElement(react.Fragment, null, dicomFileUploader.getStatus() === UploadStatus.InProgress && /*#__PURE__*/react.createElement("div", {
    className: "w-10 text-right"
  }, percentComplete, "%"), /*#__PURE__*/react.createElement("div", {
    className: "ml-auto flex cursor-pointer"
  }, /*#__PURE__*/react.createElement(ui_src/* Icon */.JO, {
    className: "text-primary-active self-center",
    name: "close",
    onClick: cancelUpload
  })))));
});
DicomUploadProgressItem.propTypes = {
  dicomFileUploader: prop_types_default().instanceOf(DicomFileUploader).isRequired
};
/* harmony default export */ const DicomUpload_DicomUploadProgressItem = (DicomUploadProgressItem);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/components/DicomUpload/DicomUploadProgress.tsx






const ONE_SECOND = 1000;
const ONE_MINUTE = ONE_SECOND * 60;
const ONE_HOUR = ONE_MINUTE * 60;

// The base/initial interval time length used to calculate the
// rate of the upload and in turn estimate the
// the amount of time remaining for the upload. This is the length
// of the very first interval to get a reasonable estimate on screen in
// a reasonable amount of time. The length of each interval after the first
// is based on the upload rate calculated. Faster rates use this base interval
// length. Slower rates below UPLOAD_RATE_THRESHOLD get longer interval times
// to obtain more accurate upload rates.
const BASE_INTERVAL_TIME = 15000;

// The upload rate threshold to determine the length of the interval to
// calculate the upload rate.
const UPLOAD_RATE_THRESHOLD = 75;
const NO_WRAP_ELLIPSIS_CLASS_NAMES = 'text-ellipsis whitespace-nowrap overflow-hidden';
function DicomUploadProgress(_ref) {
  let {
    dicomFileUploaderArr,
    onComplete
  } = _ref;
  const [totalUploadSize] = (0,react.useState)(dicomFileUploaderArr.reduce((acc, fileUploader) => acc + fileUploader.getFileSize(), 0));
  const currentUploadSizeRef = (0,react.useRef)(0);
  const uploadRateRef = (0,react.useRef)(0);
  const [timeRemaining, setTimeRemaining] = (0,react.useState)(null);
  const [percentComplete, setPercentComplete] = (0,react.useState)(0);
  const [numFilesCompleted, setNumFilesCompleted] = (0,react.useState)(0);
  const [numFails, setNumFails] = (0,react.useState)(0);
  const [showFailedOnly, setShowFailedOnly] = (0,react.useState)(false);
  const progressBarContainerRef = (0,react.useRef)();

  /**
   * The effect for measuring and setting the current upload rate. This is
   * done by measuring the amount of data uploaded in a set interval time.
   */
  (0,react.useEffect)(() => {
    let timeoutId;

    // The amount of data already uploaded at the start of the interval.
    let intervalStartUploadSize = 0;

    // The starting time of the interval.
    let intervalStartTime = Date.now();
    const setUploadRateRef = () => {
      const uploadSizeFromStartOfInterval = currentUploadSizeRef.current - intervalStartUploadSize;
      const now = Date.now();
      const timeSinceStartOfInterval = now - intervalStartTime;

      // Calculate and set the upload rate (ref)
      uploadRateRef.current = uploadSizeFromStartOfInterval / timeSinceStartOfInterval;

      // Reset the interval starting values.
      intervalStartUploadSize = currentUploadSizeRef.current;
      intervalStartTime = now;

      // Only start a new interval if there is more to upload.
      if (totalUploadSize - currentUploadSizeRef.current > 0) {
        if (uploadRateRef.current >= UPLOAD_RATE_THRESHOLD) {
          timeoutId = setTimeout(setUploadRateRef, BASE_INTERVAL_TIME);
        } else {
          // The current upload rate is relatively slow, so use a larger
          // time interval to get a better upload rate estimate.
          timeoutId = setTimeout(setUploadRateRef, BASE_INTERVAL_TIME * 2);
        }
      }
    };

    // The very first interval is just the base time interval length.
    timeoutId = setTimeout(setUploadRateRef, BASE_INTERVAL_TIME);
    return () => {
      clearTimeout(timeoutId);
    };
  }, []);

  /**
   * The effect for: updating the overall percentage complete; setting the
   * estimated time remaining; updating the number of files uploaded; and
   * detecting if any error has occurred.
   */
  (0,react.useEffect)(() => {
    let currentTimeRemaining = null;

    // For each uploader, listen for the progress percentage complete and
    // add promise catch/finally callbacks to detect errors and count number
    // of uploads complete.
    const subscriptions = dicomFileUploaderArr.map(fileUploader => {
      let currentFileUploadSize = 0;
      const updateProgress = percentComplete => {
        const previousFileUploadSize = currentFileUploadSize;
        currentFileUploadSize = Math.round(percentComplete / 100 * fileUploader.getFileSize());
        currentUploadSizeRef.current = Math.min(totalUploadSize, currentUploadSizeRef.current - previousFileUploadSize + currentFileUploadSize);
        setPercentComplete(currentUploadSizeRef.current / totalUploadSize * 100);
        if (uploadRateRef.current !== 0) {
          const uploadSizeRemaining = totalUploadSize - currentUploadSizeRef.current;
          const timeRemaining = Math.round(uploadSizeRemaining / uploadRateRef.current);
          if (currentTimeRemaining === null) {
            currentTimeRemaining = timeRemaining;
            setTimeRemaining(currentTimeRemaining);
            return;
          }

          // Do not show an increase in the time remaining by two seconds or minutes
          // so as to prevent jumping the time remaining up and down constantly
          // due to rounding, inaccuracies in the estimate and slight variations
          // in upload rates over time.
          if (timeRemaining < ONE_MINUTE) {
            const currentSecondsRemaining = Math.ceil(currentTimeRemaining / ONE_SECOND);
            const secondsRemaining = Math.ceil(timeRemaining / ONE_SECOND);
            const delta = secondsRemaining - currentSecondsRemaining;
            if (delta < 0 || delta > 2) {
              currentTimeRemaining = timeRemaining;
              setTimeRemaining(currentTimeRemaining);
            }
            return;
          }
          if (timeRemaining < ONE_HOUR) {
            const currentMinutesRemaining = Math.ceil(currentTimeRemaining / ONE_MINUTE);
            const minutesRemaining = Math.ceil(timeRemaining / ONE_MINUTE);
            const delta = minutesRemaining - currentMinutesRemaining;
            if (delta < 0 || delta > 2) {
              currentTimeRemaining = timeRemaining;
              setTimeRemaining(currentTimeRemaining);
            }
            return;
          }

          // Hours remaining...
          currentTimeRemaining = timeRemaining;
          setTimeRemaining(currentTimeRemaining);
        }
      };
      const progressCallback = progressEvent => {
        updateProgress(progressEvent.percentComplete);
      };

      // Use the uploader promise to flag any error and count the number of
      // uploads completed.
      fileUploader.load().catch(rejection => {
        if (rejection.status === UploadStatus.Failed) {
          setNumFails(numFails => numFails + 1);
        }
      }).finally(() => {
        // If any error occurred, the percent complete progress stops firing
        // but this call to updateProgress nicely puts all finished uploads at 100%.
        updateProgress(100);
        setNumFilesCompleted(numCompleted => numCompleted + 1);
      });
      return fileUploader.subscribe(EVENTS.PROGRESS, progressCallback);
    });
    return () => {
      subscriptions.forEach(subscription => subscription.unsubscribe());
    };
  }, []);
  const cancelAllUploads = (0,react.useCallback)(async () => {
    for (const dicomFileUploader of dicomFileUploaderArr) {
      // Important: we need a non-blocking way to cancel every upload,
      // otherwise the UI will freeze and the user will not be able
      // to interact with the app and progress will not be updated.
      const promise = new Promise((resolve, reject) => {
        setTimeout(() => {
          dicomFileUploader.cancel();
          resolve();
        }, 0);
      });
    }
  }, []);
  const getFormattedTimeRemaining = (0,react.useCallback)(() => {
    if (timeRemaining == null) {
      return '';
    }
    if (timeRemaining < ONE_MINUTE) {
      const secondsRemaining = Math.ceil(timeRemaining / ONE_SECOND);
      return `${secondsRemaining} ${secondsRemaining === 1 ? 'second' : 'seconds'}`;
    }
    if (timeRemaining < ONE_HOUR) {
      const minutesRemaining = Math.ceil(timeRemaining / ONE_MINUTE);
      return `${minutesRemaining} ${minutesRemaining === 1 ? 'minute' : 'minutes'}`;
    }
    const hoursRemaining = Math.ceil(timeRemaining / ONE_HOUR);
    return `${hoursRemaining} ${hoursRemaining === 1 ? 'hour' : 'hours'}`;
  }, [timeRemaining]);
  const getPercentCompleteRounded = (0,react.useCallback)(() => Math.min(100, Math.round(percentComplete)), [percentComplete]);

  /**
   * Determines if the progress bar should show the infinite animation or not.
   * Show the infinite animation for progress less than 1% AND if less than
   * one pixel of the progress bar would be displayed.
   */
  const showInfiniteProgressBar = (0,react.useCallback)(() => {
    return getPercentCompleteRounded() < 1 && (progressBarContainerRef?.current?.offsetWidth ?? 0) * (percentComplete / 100) < 1;
  }, [getPercentCompleteRounded, percentComplete]);

  /**
   * Gets the css style for the 'n of m' (files completed) text. The only css attribute
   * of the style is width such that the 'n of m' is always a fixed width and thus
   * as each file completes uploading the text on screen does not constantly shift
   * left and right.
   */
  const getNofMFilesStyle = (0,react.useCallback)(() => {
    // the number of digits accounts for the digits being on each side of the ' of '
    const numDigits = 2 * dicomFileUploaderArr.length.toString().length;
    // the number of digits + 2 spaces and 2 characters for ' of '
    const numChars = numDigits + 4;
    return {
      width: `${numChars}ch`
    };
  }, []);
  const getNumCompletedAndTimeRemainingComponent = () => {
    return /*#__PURE__*/react.createElement("div", {
      className: "bg-primary-dark flex h-14 items-center px-1 pb-4 text-lg"
    }, numFilesCompleted === dicomFileUploaderArr.length ? /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("span", {
      className: NO_WRAP_ELLIPSIS_CLASS_NAMES
    }, `${dicomFileUploaderArr.length} ${dicomFileUploaderArr.length > 1 ? 'files' : 'file'} completed.`), /*#__PURE__*/react.createElement(ui_src/* Button */.zx, {
      disabled: false,
      className: "ml-auto",
      onClick: onComplete
    }, 'Close')) : /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("span", {
      style: getNofMFilesStyle(),
      className: classnames_default()(NO_WRAP_ELLIPSIS_CLASS_NAMES, 'text-end')
    }, `${numFilesCompleted} of ${dicomFileUploaderArr.length}`, "\xA0"), /*#__PURE__*/react.createElement("span", {
      className: NO_WRAP_ELLIPSIS_CLASS_NAMES
    }, ' files completed.', "\xA0"), /*#__PURE__*/react.createElement("span", {
      className: NO_WRAP_ELLIPSIS_CLASS_NAMES
    }, timeRemaining ? `Less than ${getFormattedTimeRemaining()} remaining. ` : ''), /*#__PURE__*/react.createElement("span", {
      className: classnames_default()(NO_WRAP_ELLIPSIS_CLASS_NAMES, 'text-primary-active hover:text-primary-light active:text-aqua-pale ml-auto cursor-pointer'),
      onClick: cancelAllUploads
    }, "Cancel All Uploads")));
  };
  const getShowFailedOnlyIconComponent = () => {
    return /*#__PURE__*/react.createElement("div", {
      className: "ml-auto flex w-6 justify-center"
    }, numFails > 0 && /*#__PURE__*/react.createElement("div", {
      onClick: () => setShowFailedOnly(currentShowFailedOnly => !currentShowFailedOnly)
    }, /*#__PURE__*/react.createElement(ui_src/* Icon */.JO, {
      className: "cursor-pointer",
      name: "icon-status-alert"
    })));
  };
  const getPercentCompleteComponent = () => {
    return /*#__PURE__*/react.createElement("div", {
      className: "ohif-scrollbar border-secondary-light overflow-y-scroll border-b px-2"
    }, /*#__PURE__*/react.createElement("div", {
      className: "min-h-14 flex w-full items-center p-2.5"
    }, numFilesCompleted === dicomFileUploaderArr.length ? /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("div", {
      className: "text-primary-light text-xl"
    }, numFails > 0 ? `Completed with ${numFails} ${numFails > 1 ? 'errors' : 'error'}!` : 'Completed!'), getShowFailedOnlyIconComponent()) : /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("div", {
      ref: progressBarContainerRef,
      className: "flex-grow"
    }, /*#__PURE__*/react.createElement(ui_src/* ProgressLoadingBar */.YE, {
      progress: showInfiniteProgressBar() ? undefined : Math.min(100, percentComplete)
    })), /*#__PURE__*/react.createElement("div", {
      className: "ml-1 flex w-24 items-center"
    }, /*#__PURE__*/react.createElement("div", {
      className: "w-10 text-right"
    }, `${getPercentCompleteRounded()}%`), getShowFailedOnlyIconComponent()))));
  };
  return /*#__PURE__*/react.createElement("div", {
    className: "flex grow flex-col"
  }, getNumCompletedAndTimeRemainingComponent(), /*#__PURE__*/react.createElement("div", {
    className: "flex grow flex-col overflow-hidden bg-black text-lg"
  }, getPercentCompleteComponent(), /*#__PURE__*/react.createElement("div", {
    className: "ohif-scrollbar h-1 grow overflow-y-scroll px-2"
  }, dicomFileUploaderArr.filter(dicomFileUploader => !showFailedOnly || dicomFileUploader.getStatus() === UploadStatus.Failed).map(dicomFileUploader => /*#__PURE__*/react.createElement(DicomUpload_DicomUploadProgressItem, {
    key: dicomFileUploader.getFileId(),
    dicomFileUploader: dicomFileUploader
  })))));
}
DicomUploadProgress.propTypes = {
  dicomFileUploaderArr: prop_types_default().arrayOf(prop_types_default().instanceOf(DicomFileUploader)).isRequired,
  onComplete: (prop_types_default()).func.isRequired
};
/* harmony default export */ const DicomUpload_DicomUploadProgress = (DicomUploadProgress);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/components/DicomUpload/DicomUpload.css
// extracted by mini-css-extract-plugin

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/components/DicomUpload/DicomUpload.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }








function DicomUpload(_ref) {
  let {
    dataSource,
    onComplete,
    onStarted
  } = _ref;
  const baseClassNames = 'min-h-[480px] flex flex-col bg-black select-none';
  const [dicomFileUploaderArr, setDicomFileUploaderArr] = (0,react.useState)([]);
  const onDrop = (0,react.useCallback)(async acceptedFiles => {
    onStarted();
    setDicomFileUploaderArr(acceptedFiles.map(file => new DicomFileUploader(file, dataSource)));
  }, []);
  const getDropZoneComponent = () => {
    return /*#__PURE__*/react.createElement(es/* default */.Z, {
      onDrop: acceptedFiles => {
        onDrop(acceptedFiles);
      },
      noClick: true
    }, _ref2 => {
      let {
        getRootProps
      } = _ref2;
      return /*#__PURE__*/react.createElement("div", _extends({}, getRootProps(), {
        className: "dicom-upload-drop-area-border-dash m-5 flex h-full flex-col items-center justify-center"
      }), /*#__PURE__*/react.createElement("div", {
        className: "flex gap-3"
      }, /*#__PURE__*/react.createElement(es/* default */.Z, {
        onDrop: onDrop,
        noDrag: true
      }, _ref3 => {
        let {
          getRootProps,
          getInputProps
        } = _ref3;
        return /*#__PURE__*/react.createElement("div", getRootProps(), /*#__PURE__*/react.createElement(ui_src/* Button */.zx, {
          disabled: false,
          onClick: () => {}
        }, 'Add files', /*#__PURE__*/react.createElement("input", getInputProps())));
      }), /*#__PURE__*/react.createElement(es/* default */.Z, {
        onDrop: onDrop,
        noDrag: true
      }, _ref4 => {
        let {
          getRootProps,
          getInputProps
        } = _ref4;
        return /*#__PURE__*/react.createElement("div", getRootProps(), /*#__PURE__*/react.createElement(ui_src/* Button */.zx, {
          type: ui_src/* ButtonEnums.type */.LZ.dt.secondary,
          disabled: false,
          onClick: () => {}
        }, 'Add folder', /*#__PURE__*/react.createElement("input", _extends({}, getInputProps(), {
          webkitdirectory: "true",
          mozdirectory: "true"
        }))));
      })), /*#__PURE__*/react.createElement("div", {
        className: "pt-5"
      }, "or drag images or folders here"), /*#__PURE__*/react.createElement("div", {
        className: "text-aqua-pale pt-3 text-lg"
      }, "(DICOM files supported)"));
    });
  };
  return /*#__PURE__*/react.createElement(react.Fragment, null, dicomFileUploaderArr.length ? /*#__PURE__*/react.createElement("div", {
    className: classnames_default()('h-[calc(100vh-300px)]', baseClassNames)
  }, /*#__PURE__*/react.createElement(DicomUpload_DicomUploadProgress, {
    dicomFileUploaderArr: Array.from(dicomFileUploaderArr),
    onComplete: onComplete
  })) : /*#__PURE__*/react.createElement("div", {
    className: classnames_default()('h-[480px]', baseClassNames)
  }, getDropZoneComponent()));
}
DicomUpload.propTypes = {
  dataSource: (prop_types_default()).object.isRequired,
  onComplete: (prop_types_default()).func.isRequired,
  onStarted: (prop_types_default()).func.isRequired
};
/* harmony default export */ const DicomUpload_DicomUpload = (DicomUpload);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/getCustomizationModule.ts



const tools = {
  active: [{
    toolName: toolNames.WindowLevel,
    bindings: [{
      mouseButton: dist_esm.Enums.MouseBindings.Primary
    }]
  }, {
    toolName: toolNames.Pan,
    bindings: [{
      mouseButton: dist_esm.Enums.MouseBindings.Auxiliary
    }]
  }, {
    toolName: toolNames.Zoom,
    bindings: [{
      mouseButton: dist_esm.Enums.MouseBindings.Secondary
    }]
  }, {
    toolName: toolNames.StackScrollMouseWheel,
    bindings: []
  }],
  enabled: [{
    toolName: toolNames.SegmentationDisplay
  }]
};
function getCustomizationModule() {
  return [{
    name: 'cornerstoneDicomUploadComponent',
    value: {
      id: 'dicomUploadComponent',
      component: DicomUpload_DicomUpload
    }
  }, {
    name: 'default',
    value: [{
      id: 'cornerstone.overlayViewportTools',
      tools
    }]
  }];
}
/* harmony default export */ const src_getCustomizationModule = (getCustomizationModule);
// EXTERNAL MODULE: ../../../node_modules/html2canvas/dist/html2canvas.esm.js
var html2canvas_esm = __webpack_require__(76010);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/CornerstoneViewportDownloadForm.tsx







const MINIMUM_SIZE = 100;
const DEFAULT_SIZE = 512;
const MAX_TEXTURE_SIZE = 10000;
const VIEWPORT_ID = 'cornerstone-viewport-download-form';
const CornerstoneViewportDownloadForm = _ref => {
  let {
    onClose,
    activeViewportId: activeViewportIdProp,
    cornerstoneViewportService
  } = _ref;
  const enabledElement = (0,state/* getEnabledElement */.K8)(activeViewportIdProp);
  const activeViewportElement = enabledElement?.element;
  const activeViewportEnabledElement = (0,esm.getEnabledElement)(activeViewportElement);
  const {
    viewportId: activeViewportId,
    renderingEngineId
  } = activeViewportEnabledElement;
  const toolGroup = dist_esm.ToolGroupManager.getToolGroupForViewport(activeViewportId, renderingEngineId);
  const toolModeAndBindings = Object.keys(toolGroup.toolOptions).reduce((acc, toolName) => {
    const tool = toolGroup.toolOptions[toolName];
    const {
      mode,
      bindings
    } = tool;
    return {
      ...acc,
      [toolName]: {
        mode,
        bindings
      }
    };
  }, {});
  (0,react.useEffect)(() => {
    return () => {
      Object.keys(toolModeAndBindings).forEach(toolName => {
        const {
          mode,
          bindings
        } = toolModeAndBindings[toolName];
        toolGroup.setToolMode(toolName, mode, {
          bindings
        });
      });
    };
  }, []);
  const enableViewport = viewportElement => {
    if (viewportElement) {
      const {
        renderingEngine,
        viewport
      } = (0,esm.getEnabledElement)(activeViewportElement);
      const viewportInput = {
        viewportId: VIEWPORT_ID,
        element: viewportElement,
        type: viewport.type,
        defaultOptions: {
          background: viewport.defaultOptions.background,
          orientation: viewport.defaultOptions.orientation
        }
      };
      renderingEngine.enableElement(viewportInput);
    }
  };
  const disableViewport = viewportElement => {
    if (viewportElement) {
      const {
        renderingEngine
      } = (0,esm.getEnabledElement)(viewportElement);
      return new Promise(resolve => {
        renderingEngine.disableElement(VIEWPORT_ID);
      });
    }
  };
  const updateViewportPreview = (downloadViewportElement, internalCanvas, fileType) => new Promise(resolve => {
    const enabledElement = (0,esm.getEnabledElement)(downloadViewportElement);
    const {
      viewport: downloadViewport,
      renderingEngine
    } = enabledElement;

    // Note: Since any trigger of dimensions will update the viewport,
    // we need to resize the offScreenCanvas to accommodate for the new
    // dimensions, this is due to the reason that we are using the GPU offScreenCanvas
    // to render the viewport for the downloadViewport.
    renderingEngine.resize();

    // Trigger the render on the viewport to update the on screen
    downloadViewport.render();
    downloadViewportElement.addEventListener(esm.Enums.Events.IMAGE_RENDERED, function updateViewport(event) {
      const enabledElement = (0,esm.getEnabledElement)(event.target);
      const {
        viewport
      } = enabledElement;
      const {
        element
      } = viewport;
      const downloadCanvas = (0,esm.getOrCreateCanvas)(element);
      const type = 'image/' + fileType;
      const dataUrl = downloadCanvas.toDataURL(type, 1);
      let newWidth = element.offsetHeight;
      let newHeight = element.offsetWidth;
      if (newWidth > DEFAULT_SIZE || newHeight > DEFAULT_SIZE) {
        const multiplier = DEFAULT_SIZE / Math.max(newWidth, newHeight);
        newHeight *= multiplier;
        newWidth *= multiplier;
      }
      resolve({
        dataUrl,
        width: newWidth,
        height: newHeight
      });
      downloadViewportElement.removeEventListener(esm.Enums.Events.IMAGE_RENDERED, updateViewport);
    });
  });
  const loadImage = (activeViewportElement, viewportElement, width, height) => new Promise(resolve => {
    if (activeViewportElement && viewportElement) {
      const activeViewportEnabledElement = (0,esm.getEnabledElement)(activeViewportElement);
      if (!activeViewportEnabledElement) {
        return;
      }
      const {
        viewport
      } = activeViewportEnabledElement;
      const renderingEngine = cornerstoneViewportService.getRenderingEngine();
      const downloadViewport = renderingEngine.getViewport(VIEWPORT_ID);
      if (downloadViewport instanceof esm.StackViewport) {
        const imageId = viewport.getCurrentImageId();
        const properties = viewport.getProperties();
        downloadViewport.setStack([imageId]).then(() => {
          try {
            downloadViewport.setProperties(properties);
            const newWidth = Math.min(width || image.width, MAX_TEXTURE_SIZE);
            const newHeight = Math.min(height || image.height, MAX_TEXTURE_SIZE);
            resolve({
              width: newWidth,
              height: newHeight
            });
          } catch (e) {
            // Happens on clicking the cancel button
            console.warn('Unable to set properties', e);
          }
        });
      } else if (downloadViewport instanceof esm.VolumeViewport) {
        const actors = viewport.getActors();
        // downloadViewport.setActors(actors);
        actors.forEach(actor => {
          downloadViewport.addActor(actor);
        });
        downloadViewport.setCamera(viewport.getCamera());
        downloadViewport.render();
        const newWidth = Math.min(width || image.width, MAX_TEXTURE_SIZE);
        const newHeight = Math.min(height || image.height, MAX_TEXTURE_SIZE);
        resolve({
          width: newWidth,
          height: newHeight
        });
      }
    }
  });
  const toggleAnnotations = (toggle, viewportElement, activeViewportElement) => {
    const activeViewportEnabledElement = (0,esm.getEnabledElement)(activeViewportElement);
    const downloadViewportElement = (0,esm.getEnabledElement)(viewportElement);
    const {
      viewportId: activeViewportId,
      renderingEngineId
    } = activeViewportEnabledElement;
    const {
      viewportId: downloadViewportId
    } = downloadViewportElement;
    if (!activeViewportEnabledElement || !downloadViewportElement) {
      return;
    }
    const toolGroup = dist_esm.ToolGroupManager.getToolGroupForViewport(activeViewportId, renderingEngineId);

    // add the viewport to the toolGroup
    toolGroup.addViewport(downloadViewportId, renderingEngineId);
    Object.keys(toolGroup._toolInstances).forEach(toolName => {
      // make all tools Enabled so that they can not be interacted with
      // in the download viewport
      if (toggle && toolName !== 'Crosshairs') {
        try {
          toolGroup.setToolEnabled(toolName);
        } catch (e) {
          console.log(e);
        }
      } else {
        toolGroup.setToolDisabled(toolName);
      }
    });
  };
  const downloadBlob = (filename, fileType) => {
    const file = `${filename}.${fileType}`;
    const divForDownloadViewport = document.querySelector(`div[data-viewport-uid="${VIEWPORT_ID}"]`);
    (0,html2canvas_esm/* default */.Z)(divForDownloadViewport).then(canvas => {
      const link = document.createElement('a');
      link.download = file;
      link.href = canvas.toDataURL(fileType, 1.0);
      link.click();
    });
  };
  return /*#__PURE__*/react.createElement(ui_src/* ViewportDownloadForm */.mQ, {
    onClose: onClose,
    minimumSize: MINIMUM_SIZE,
    maximumSize: MAX_TEXTURE_SIZE,
    defaultSize: DEFAULT_SIZE,
    canvasClass: 'cornerstone-canvas',
    activeViewportElement: activeViewportElement,
    enableViewport: enableViewport,
    disableViewport: disableViewport,
    updateViewportPreview: updateViewportPreview,
    loadImage: loadImage,
    toggleAnnotations: toggleAnnotations,
    downloadBlob: downloadBlob
  });
};
CornerstoneViewportDownloadForm.propTypes = {
  onClose: (prop_types_default()).func,
  activeViewportId: (prop_types_default()).string.isRequired
};
/* harmony default export */ const utils_CornerstoneViewportDownloadForm = (CornerstoneViewportDownloadForm);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/stackSync/toggleStackImageSync.ts
const STACK_SYNC_NAME = 'stackImageSync';
function toggleStackImageSync(_ref) {
  let {
    toggledState,
    servicesManager,
    viewports: providedViewports
  } = _ref;
  if (!toggledState) {
    return disableSync(STACK_SYNC_NAME, servicesManager);
  }
  const {
    syncGroupService,
    viewportGridService,
    displaySetService,
    cornerstoneViewportService
  } = servicesManager.services;
  const viewports = providedViewports || getReconstructableStackViewports(viewportGridService, displaySetService);

  // create synchronization group and add the viewports to it.
  viewports.forEach(gridViewport => {
    const {
      viewportId
    } = gridViewport.viewportOptions;
    const viewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
    if (!viewport) {
      return;
    }
    syncGroupService.addViewportToSyncGroup(viewportId, viewport.getRenderingEngine().id, {
      type: 'stackimage',
      id: STACK_SYNC_NAME,
      source: true,
      target: true
    });
  });
}
function disableSync(syncName, servicesManager) {
  const {
    syncGroupService,
    viewportGridService,
    displaySetService,
    cornerstoneViewportService
  } = servicesManager.services;
  const viewports = getReconstructableStackViewports(viewportGridService, displaySetService);
  viewports.forEach(gridViewport => {
    const {
      viewportId
    } = gridViewport.viewportOptions;
    const viewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
    if (!viewport) {
      return;
    }
    syncGroupService.removeViewportFromSyncGroup(viewport.id, viewport.getRenderingEngine().id, syncName);
  });
}

/**
 * Gets the consistent spacing stack viewport types, which are the ones which
 * can be navigated using the stack image sync right now.
 */
function getReconstructableStackViewports(viewportGridService, displaySetService) {
  let {
    viewports
  } = viewportGridService.getState();
  viewports = [...viewports.values()];
  // filter empty viewports
  viewports = viewports.filter(viewport => viewport.displaySetInstanceUIDs && viewport.displaySetInstanceUIDs.length);

  // filter reconstructable viewports
  viewports = viewports.filter(viewport => {
    const {
      displaySetInstanceUIDs
    } = viewport;
    for (const displaySetInstanceUID of displaySetInstanceUIDs) {
      const displaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);

      // TODO - add a better test than isReconstructable
      if (displaySet && displaySet.isReconstructable) {
        return true;
      }
      return false;
    }
  });
  return viewports;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/commandsModule.ts







function commandsModule(_ref) {
  let {
    servicesManager,
    commandsManager
  } = _ref;
  const {
    viewportGridService,
    toolGroupService,
    cineService,
    toolbarService,
    uiDialogService,
    cornerstoneViewportService,
    uiNotificationService,
    measurementService
  } = servicesManager.services;
  const {
    measurementServiceSource
  } = this;
  function _getActiveViewportEnabledElement() {
    return getActiveViewportEnabledElement(viewportGridService);
  }
  const actions = {
    /**
     * Generates the selector props for the context menu, specific to
     * the cornerstone viewport, and then runs the context menu.
     */
    showCornerstoneContextMenu: options => {
      const element = _getActiveViewportEnabledElement()?.viewport?.element;
      const optionsToUse = {
        ...options,
        element
      };
      const {
        useSelectedAnnotation,
        nearbyToolData,
        event
      } = optionsToUse;

      // This code is used to invoke the context menu via keyboard shortcuts
      if (useSelectedAnnotation && !nearbyToolData) {
        const firstAnnotationSelected = getFirstAnnotationSelected(element);
        // filter by allowed selected tools from config property (if there is any)
        const isToolAllowed = !optionsToUse.allowedSelectedTools || optionsToUse.allowedSelectedTools.includes(firstAnnotationSelected?.metadata?.toolName);
        if (isToolAllowed) {
          optionsToUse.nearbyToolData = firstAnnotationSelected;
        } else {
          return;
        }
      }
      optionsToUse.defaultPointsPosition = [];
      // if (optionsToUse.nearbyToolData) {
      //   optionsToUse.defaultPointsPosition = commandsManager.runCommand(
      //     'getToolDataActiveCanvasPoints',
      //     { toolData: optionsToUse.nearbyToolData }
      //   );
      // }

      // TODO - make the selectorProps richer by including the study metadata and display set.
      optionsToUse.selectorProps = {
        toolName: optionsToUse.nearbyToolData?.metadata?.toolName,
        value: optionsToUse.nearbyToolData,
        uid: optionsToUse.nearbyToolData?.annotationUID,
        nearbyToolData: optionsToUse.nearbyToolData,
        event,
        ...optionsToUse.selectorProps
      };
      commandsManager.run(options, optionsToUse);
    },
    getNearbyToolData(_ref2) {
      let {
        nearbyToolData,
        element,
        canvasCoordinates
      } = _ref2;
      return nearbyToolData ?? dist_esm.utilities.getAnnotationNearPoint(element, canvasCoordinates);
    },
    getNearbyAnnotation(_ref3) {
      let {
        element,
        canvasCoordinates
      } = _ref3;
      const nearbyToolData = actions.getNearbyToolData({
        nearbyToolData: null,
        element,
        canvasCoordinates
      });
      const isAnnotation = toolName => {
        const enabledElement = (0,esm.getEnabledElement)(element);
        if (!enabledElement) {
          return;
        }
        const {
          renderingEngineId,
          viewportId
        } = enabledElement;
        const toolGroup = dist_esm.ToolGroupManager.getToolGroupForViewport(viewportId, renderingEngineId);
        const toolInstance = toolGroup.getToolInstance(toolName);
        return toolInstance?.constructor?.isAnnotation ?? true;
      };
      return nearbyToolData?.metadata?.toolName && isAnnotation(nearbyToolData.metadata.toolName) ? nearbyToolData : null;
    },
    // Measurement tool commands:

    /** Delete the given measurement */
    deleteMeasurement: _ref4 => {
      let {
        uid
      } = _ref4;
      if (uid) {
        measurementServiceSource.remove(uid);
      }
    },
    /**
     * Show the measurement labelling input dialog and update the label
     * on the measurement with a response if not cancelled.
     */
    setMeasurementLabel: _ref5 => {
      let {
        uid
      } = _ref5;
      const measurement = measurementService.getMeasurement(uid);
      utils_callInputDialog(uiDialogService, measurement, (label, actionId) => {
        if (actionId === 'cancel') {
          return;
        }
        const updatedMeasurement = Object.assign({}, measurement, {
          label
        });
        measurementService.update(updatedMeasurement.uid, updatedMeasurement, true);
      }, false);
    },
    /**
     *
     * @param props - containing the updates to apply
     * @param props.measurementKey - chooses the measurement key to apply the
     *        code to.  This will typically be finding or site to apply a
     *        finding code or a findingSites code.
     * @param props.code - A coding scheme value from DICOM, including:
     *       * CodeValue - the language independent code, for example '1234'
     *       * CodingSchemeDesignator - the issue of the code value
     *       * CodeMeaning - the text value shown to the user
     *       * ref - a string reference in the form `<designator>:<codeValue>`
     *       * Other fields
     *     Note it is a valid option to remove the finding or site values by
     *     supplying null for the code.
     * @param props.uid - the measurement UID to find it with
     * @param props.label - the text value for the code.  Has NOTHING to do with
     *        the measurement label, which can be set with textLabel
     * @param props.textLabel is the measurement label to apply.  Set to null to
     *            delete.
     *
     * If the measurementKey is `site`, then the code will also be added/replace
     * the 0 element of findingSites.  This behaviour is expected to be enhanced
     * in the future with ability to set other site information.
     */
    updateMeasurement: props => {
      const {
        code,
        uid,
        textLabel,
        label
      } = props;
      const measurement = measurementService.getMeasurement(uid);
      const updatedMeasurement = {
        ...measurement
      };
      // Call it textLabel as the label value
      // TODO - remove the label setting when direct rendering of findingSites is enabled
      if (textLabel !== undefined) {
        updatedMeasurement.label = textLabel;
      }
      if (code !== undefined) {
        const measurementKey = code.type || 'finding';
        if (code.ref && !code.CodeValue) {
          const split = code.ref.indexOf(':');
          code.CodeValue = code.ref.substring(split + 1);
          code.CodeMeaning = code.text || label;
          code.CodingSchemeDesignator = code.ref.substring(0, split);
        }
        updatedMeasurement[measurementKey] = code;
        // TODO - remove this line once the measurements table customizations are in
        if (measurementKey !== 'finding') {
          if (updatedMeasurement.findingSites) {
            updatedMeasurement.findingSites = updatedMeasurement.findingSites.filter(it => it.type !== measurementKey);
            updatedMeasurement.findingSites.push(code);
          } else {
            updatedMeasurement.findingSites = [code];
          }
        }
      }
      measurementService.update(updatedMeasurement.uid, updatedMeasurement, true);
    },
    // Retrieve value commands
    getActiveViewportEnabledElement: _getActiveViewportEnabledElement,
    setViewportActive: _ref6 => {
      let {
        viewportId
      } = _ref6;
      const viewportInfo = cornerstoneViewportService.getViewportInfo(viewportId);
      if (!viewportInfo) {
        console.warn('No viewport found for viewportId:', viewportId);
        return;
      }
      viewportGridService.setActiveViewportId(viewportId);
    },
    arrowTextCallback: _ref7 => {
      let {
        callback,
        data
      } = _ref7;
      utils_callInputDialog(uiDialogService, data, callback);
    },
    cleanUpCrosshairs: () => {
      // if the crosshairs tool is active, deactivate it and set window level active
      // since we are going back to main non-mpr HP
      const activeViewportToolGroup = toolGroupService.getToolGroup(null);
      if (activeViewportToolGroup._toolInstances?.Crosshairs?.mode === dist_esm.Enums.ToolModes.Active) {
        actions.toolbarServiceRecordInteraction({
          interactionType: 'tool',
          commands: [{
            commandOptions: {
              toolName: 'WindowLevel'
            },
            context: 'CORNERSTONE'
          }]
        });
      }
    },
    toggleCine: () => {
      const {
        viewports
      } = viewportGridService.getState();
      const {
        isCineEnabled
      } = cineService.getState();
      cineService.setIsCineEnabled(!isCineEnabled);
      toolbarService.setButton('Cine', {
        props: {
          isActive: !isCineEnabled
        }
      });
      viewports.forEach((_, index) => cineService.setCine({
        id: index,
        isPlaying: false
      }));
    },
    setWindowLevel(_ref8) {
      let {
        window,
        level,
        toolGroupId
      } = _ref8;
      // convert to numbers
      const windowWidthNum = Number(window);
      const windowCenterNum = Number(level);
      const {
        viewportId
      } = _getActiveViewportEnabledElement();
      const viewportToolGroupId = toolGroupService.getToolGroupForViewport(viewportId);
      if (toolGroupId && toolGroupId !== viewportToolGroupId) {
        return;
      }

      // get actor from the viewport
      const renderingEngine = cornerstoneViewportService.getRenderingEngine();
      const viewport = renderingEngine.getViewport(viewportId);
      const {
        lower,
        upper
      } = esm.utilities.windowLevel.toLowHighRange(windowWidthNum, windowCenterNum);
      viewport.setProperties({
        voiRange: {
          upper,
          lower
        }
      });
      viewport.render();
    },
    // Just call the toolbar service record interaction - allows
    // executing a toolbar command as a full toolbar command with side affects
    // coming from the ToolbarService itself.
    toolbarServiceRecordInteraction: props => {
      toolbarService.recordInteraction(props);
    },
    // Enable or disable a toggleable command, without calling the activation
    // Used to setup already active tools from hanging protocols
    setToolbarToggled: props => {
      toolbarService.setToggled(props.toolId, props.isActive ?? true);
    },
    setToolActive: _ref9 => {
      let {
        toolName,
        toolGroupId = null,
        toggledState
      } = _ref9;
      if (toolName === 'Crosshairs') {
        const activeViewportToolGroup = toolGroupService.getToolGroup(null);
        if (!activeViewportToolGroup._toolInstances.Crosshairs) {
          uiNotificationService.show({
            title: 'Crosshairs',
            message: 'You need to be in a MPR view to use Crosshairs. Click on MPR button in the toolbar to activate it.',
            type: 'info',
            duration: 3000
          });
          throw new Error('Crosshairs tool is not available in this viewport');
        }
      }
      const {
        viewports
      } = viewportGridService.getState();
      if (!viewports.size) {
        return;
      }
      const toolGroup = toolGroupService.getToolGroup(toolGroupId);
      if (!toolGroup) {
        return;
      }
      if (!toolGroup.getToolInstance(toolName)) {
        uiNotificationService.show({
          title: `${toolName} tool`,
          message: `The ${toolName} tool is not available in this viewport.`,
          type: 'info',
          duration: 3000
        });
        throw new Error(`ToolGroup ${toolGroup.id} does not have this tool.`);
      }
      const activeToolName = toolGroup.getActivePrimaryMouseButtonTool();
      if (activeToolName) {
        // Todo: this is a hack to prevent the crosshairs to stick around
        // after another tool is selected. We should find a better way to do this
        if (activeToolName === 'Crosshairs') {
          toolGroup.setToolDisabled(activeToolName);
        } else {
          toolGroup.setToolPassive(activeToolName);
        }
      }

      // If there is a toggle state, then simply set the enabled/disabled state without
      // setting the tool active.
      if (toggledState != null) {
        toggledState ? toolGroup.setToolEnabled(toolName) : toolGroup.setToolDisabled(toolName);
        return;
      }

      // Set the new toolName to be active
      toolGroup.setToolActive(toolName, {
        bindings: [{
          mouseButton: dist_esm.Enums.MouseBindings.Primary
        }]
      });
    },
    showDownloadViewportModal: () => {
      const {
        activeViewportId
      } = viewportGridService.getState();
      if (!cornerstoneViewportService.getCornerstoneViewport(activeViewportId)) {
        // Cannot download a non-cornerstone viewport (image).
        uiNotificationService.show({
          title: 'Download Image',
          message: 'Image cannot be downloaded',
          type: 'error'
        });
        return;
      }
      const {
        uiModalService
      } = servicesManager.services;
      if (uiModalService) {
        uiModalService.show({
          content: utils_CornerstoneViewportDownloadForm,
          title: 'Download High Quality Image',
          contentProps: {
            activeViewportId,
            onClose: uiModalService.hide,
            cornerstoneViewportService
          }
        });
      }
    },
    rotateViewport: _ref10 => {
      let {
        rotation
      } = _ref10;
      const enabledElement = _getActiveViewportEnabledElement();
      if (!enabledElement) {
        return;
      }
      const {
        viewport
      } = enabledElement;
      if (viewport instanceof esm.StackViewport) {
        const {
          rotation: currentRotation
        } = viewport.getProperties();
        const newRotation = (currentRotation + rotation) % 360;
        viewport.setProperties({
          rotation: newRotation
        });
        viewport.render();
      }
    },
    flipViewportHorizontal: () => {
      const enabledElement = _getActiveViewportEnabledElement();
      if (!enabledElement) {
        return;
      }
      const {
        viewport
      } = enabledElement;
      if (viewport instanceof esm.StackViewport) {
        const {
          flipHorizontal
        } = viewport.getCamera();
        viewport.setCamera({
          flipHorizontal: !flipHorizontal
        });
        viewport.render();
      }
    },
    flipViewportVertical: () => {
      const enabledElement = _getActiveViewportEnabledElement();
      if (!enabledElement) {
        return;
      }
      const {
        viewport
      } = enabledElement;
      if (viewport instanceof esm.StackViewport) {
        const {
          flipVertical
        } = viewport.getCamera();
        viewport.setCamera({
          flipVertical: !flipVertical
        });
        viewport.render();
      }
    },
    invertViewport: _ref11 => {
      let {
        element
      } = _ref11;
      let enabledElement;
      if (element === undefined) {
        enabledElement = _getActiveViewportEnabledElement();
      } else {
        enabledElement = element;
      }
      if (!enabledElement) {
        return;
      }
      const {
        viewport
      } = enabledElement;
      const {
        invert
      } = viewport.getProperties();
      viewport.setProperties({
        invert: !invert
      });
      viewport.render();
    },
    resetViewport: () => {
      const enabledElement = _getActiveViewportEnabledElement();
      if (!enabledElement) {
        return;
      }
      const {
        viewport
      } = enabledElement;
      if (viewport instanceof esm.StackViewport) {
        viewport.resetProperties();
        viewport.resetCamera();
      } else {
        viewport.resetProperties();
        viewport.resetCamera();
      }
      viewport.render();
    },
    scaleViewport: _ref12 => {
      let {
        direction
      } = _ref12;
      const enabledElement = _getActiveViewportEnabledElement();
      const scaleFactor = direction > 0 ? 0.9 : 1.1;
      if (!enabledElement) {
        return;
      }
      const {
        viewport
      } = enabledElement;
      if (viewport instanceof esm.StackViewport) {
        if (direction) {
          const {
            parallelScale
          } = viewport.getCamera();
          viewport.setCamera({
            parallelScale: parallelScale * scaleFactor
          });
          viewport.render();
        } else {
          viewport.resetCamera();
          viewport.render();
        }
      }
    },
    /** Jumps the active viewport or the specified one to the given slice index */
    jumpToImage: _ref13 => {
      let {
        imageIndex,
        viewport: gridViewport
      } = _ref13;
      // Get current active viewport (return if none active)
      let viewport;
      if (!gridViewport) {
        const enabledElement = _getActiveViewportEnabledElement();
        if (!enabledElement) {
          return;
        }
        viewport = enabledElement.viewport;
      } else {
        viewport = cornerstoneViewportService.getCornerstoneViewport(gridViewport.id);
      }

      // Get number of slices
      // -> Copied from cornerstone3D jumpToSlice\_getImageSliceData()
      let numberOfSlices = 0;
      if (viewport instanceof esm.StackViewport) {
        numberOfSlices = viewport.getImageIds().length;
      } else if (viewport instanceof esm.VolumeViewport) {
        numberOfSlices = esm.utilities.getImageSliceDataForVolumeViewport(viewport).numberOfSlices;
      } else {
        throw new Error('Unsupported viewport type');
      }
      const jumpIndex = imageIndex < 0 ? numberOfSlices + imageIndex : imageIndex;
      if (jumpIndex >= numberOfSlices || jumpIndex < 0) {
        throw new Error(`Can't jump to ${imageIndex}`);
      }

      // Set slice to last slice
      const options = {
        imageIndex: jumpIndex
      };
      dist_esm.utilities.jumpToSlice(viewport.element, options);
    },
    scroll: _ref14 => {
      let {
        direction
      } = _ref14;
      const enabledElement = _getActiveViewportEnabledElement();
      if (!enabledElement) {
        return;
      }
      const {
        viewport
      } = enabledElement;
      const options = {
        delta: direction
      };
      dist_esm.utilities.scroll(viewport, options);
    },
    setViewportColormap: _ref15 => {
      let {
        viewportId,
        displaySetInstanceUID,
        colormap,
        immediate = false
      } = _ref15;
      const viewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
      const actorEntries = viewport.getActors();
      const actorEntry = actorEntries.find(actorEntry => {
        return actorEntry.uid.includes(displaySetInstanceUID);
      });
      const {
        actor: volumeActor,
        uid: volumeId
      } = actorEntry;
      viewport.setProperties({
        colormap,
        volumeActor
      }, volumeId);
      if (immediate) {
        viewport.render();
      }
    },
    changeActiveViewport: _ref16 => {
      let {
        direction = 1
      } = _ref16;
      const {
        activeViewportId,
        viewports
      } = viewportGridService.getState();
      const viewportIds = Array.from(viewports.keys());
      const currentIndex = viewportIds.indexOf(activeViewportId);
      const nextViewportIndex = (currentIndex + direction + viewportIds.length) % viewportIds.length;
      viewportGridService.setActiveViewportId(viewportIds[nextViewportIndex]);
    },
    toggleStackImageSync: _ref17 => {
      let {
        toggledState
      } = _ref17;
      toggleStackImageSync({
        servicesManager,
        toggledState
      });
    },
    setSourceViewportForReferenceLinesTool: _ref18 => {
      let {
        toggledState,
        viewportId
      } = _ref18;
      if (!viewportId) {
        const {
          activeViewportId
        } = viewportGridService.getState();
        viewportId = activeViewportId;
      }
      const toolGroup = toolGroupService.getToolGroupForViewport(viewportId);
      toolGroup.setToolConfiguration(dist_esm.ReferenceLinesTool.toolName, {
        sourceViewportId: viewportId
      }, true // overwrite
      );
    },

    storePresentation: _ref19 => {
      let {
        viewportId
      } = _ref19;
      cornerstoneViewportService.storePresentation({
        viewportId
      });
    }
  };
  const definitions = {
    // The command here is to show the viewer context menu, as being the
    // context menu
    showCornerstoneContextMenu: {
      commandFn: actions.showCornerstoneContextMenu,
      storeContexts: [],
      options: {
        menuCustomizationId: 'measurementsContextMenu',
        commands: [{
          commandName: 'showContextMenu'
        }]
      }
    },
    getNearbyToolData: {
      commandFn: actions.getNearbyToolData
    },
    getNearbyAnnotation: {
      commandFn: actions.getNearbyAnnotation,
      storeContexts: [],
      options: {}
    },
    deleteMeasurement: {
      commandFn: actions.deleteMeasurement
    },
    setMeasurementLabel: {
      commandFn: actions.setMeasurementLabel
    },
    updateMeasurement: {
      commandFn: actions.updateMeasurement
    },
    setWindowLevel: {
      commandFn: actions.setWindowLevel
    },
    toolbarServiceRecordInteraction: {
      commandFn: actions.toolbarServiceRecordInteraction
    },
    setToolActive: {
      commandFn: actions.setToolActive
    },
    rotateViewportCW: {
      commandFn: actions.rotateViewport,
      options: {
        rotation: 90
      }
    },
    rotateViewportCCW: {
      commandFn: actions.rotateViewport,
      options: {
        rotation: -90
      }
    },
    incrementActiveViewport: {
      commandFn: actions.changeActiveViewport
    },
    decrementActiveViewport: {
      commandFn: actions.changeActiveViewport,
      options: {
        direction: -1
      }
    },
    flipViewportHorizontal: {
      commandFn: actions.flipViewportHorizontal
    },
    flipViewportVertical: {
      commandFn: actions.flipViewportVertical
    },
    invertViewport: {
      commandFn: actions.invertViewport
    },
    resetViewport: {
      commandFn: actions.resetViewport
    },
    scaleUpViewport: {
      commandFn: actions.scaleViewport,
      options: {
        direction: 1
      }
    },
    scaleDownViewport: {
      commandFn: actions.scaleViewport,
      options: {
        direction: -1
      }
    },
    fitViewportToWindow: {
      commandFn: actions.scaleViewport,
      options: {
        direction: 0
      }
    },
    nextImage: {
      commandFn: actions.scroll,
      options: {
        direction: 1
      }
    },
    previousImage: {
      commandFn: actions.scroll,
      options: {
        direction: -1
      }
    },
    firstImage: {
      commandFn: actions.jumpToImage,
      options: {
        imageIndex: 0
      }
    },
    lastImage: {
      commandFn: actions.jumpToImage,
      options: {
        imageIndex: -1
      }
    },
    jumpToImage: {
      commandFn: actions.jumpToImage
    },
    showDownloadViewportModal: {
      commandFn: actions.showDownloadViewportModal
    },
    toggleCine: {
      commandFn: actions.toggleCine
    },
    arrowTextCallback: {
      commandFn: actions.arrowTextCallback
    },
    setViewportActive: {
      commandFn: actions.setViewportActive
    },
    setViewportColormap: {
      commandFn: actions.setViewportColormap
    },
    toggleStackImageSync: {
      commandFn: actions.toggleStackImageSync
    },
    setSourceViewportForReferenceLinesTool: {
      commandFn: actions.setSourceViewportForReferenceLinesTool
    },
    storePresentation: {
      commandFn: actions.storePresentation
    },
    setToolbarToggled: {
      commandFn: actions.setToolbarToggled
    },
    cleanUpCrosshairs: {
      commandFn: actions.cleanUpCrosshairs
    }
  };
  return {
    actions,
    definitions,
    defaultContext: 'CORNERSTONE'
  };
}
/* harmony default export */ const src_commandsModule = (commandsModule);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/getHangingProtocolModule.ts
const mpr = {
  id: 'mpr',
  name: 'Multi-Planar Reconstruction',
  locked: true,
  createdDate: '2021-02-23',
  modifiedDate: '2023-08-15',
  availableTo: {},
  editableBy: {},
  // Unknown number of priors referenced - so just match any study
  numberOfPriorsReferenced: 0,
  protocolMatchingRules: [],
  imageLoadStrategy: 'nth',
  callbacks: {
    // Switches out of MPR mode when the layout change button is used
    onLayoutChange: [{
      commandName: 'toggleHangingProtocol',
      commandOptions: {
        protocolId: 'mpr'
      },
      context: 'DEFAULT'
    }],
    // Turns off crosshairs when switching out of MPR mode
    onProtocolExit: [{
      commandName: 'cleanUpCrosshairs'
    }]
  },
  displaySetSelectors: {
    activeDisplaySet: {
      seriesMatchingRules: [{
        weight: 1,
        attribute: 'isReconstructable',
        constraint: {
          equals: {
            value: true
          }
        },
        required: true
      }]
    }
  },
  stages: [{
    name: 'MPR 1x3',
    viewportStructure: {
      layoutType: 'grid',
      properties: {
        rows: 1,
        columns: 3,
        layoutOptions: [{
          x: 0,
          y: 0,
          width: 1 / 3,
          height: 1
        }, {
          x: 1 / 3,
          y: 0,
          width: 1 / 3,
          height: 1
        }, {
          x: 2 / 3,
          y: 0,
          width: 1 / 3,
          height: 1
        }]
      }
    },
    viewports: [{
      viewportOptions: {
        viewportId: 'mpr-axial',
        toolGroupId: 'mpr',
        viewportType: 'volume',
        orientation: 'axial',
        initialImageOptions: {
          preset: 'middle'
        },
        syncGroups: [{
          type: 'voi',
          id: 'mpr',
          source: true,
          target: true
        }]
      },
      displaySets: [{
        id: 'activeDisplaySet'
      }]
    }, {
      viewportOptions: {
        viewportId: 'mpr-sagittal',
        toolGroupId: 'mpr',
        viewportType: 'volume',
        orientation: 'sagittal',
        initialImageOptions: {
          preset: 'middle'
        },
        syncGroups: [{
          type: 'voi',
          id: 'mpr',
          source: true,
          target: true
        }]
      },
      displaySets: [{
        id: 'activeDisplaySet'
      }]
    }, {
      viewportOptions: {
        viewportId: 'mpr-coronal',
        toolGroupId: 'mpr',
        viewportType: 'volume',
        orientation: 'coronal',
        initialImageOptions: {
          preset: 'middle'
        },
        syncGroups: [{
          type: 'voi',
          id: 'mpr',
          source: true,
          target: true
        }]
      },
      displaySets: [{
        id: 'activeDisplaySet'
      }]
    }]
  }]
};
const mprAnd3DVolumeViewport = {
  id: 'mprAnd3DVolumeViewport',
  locked: true,
  name: 'mpr',
  createdDate: '2023-03-15T10:29:44.894Z',
  modifiedDate: '2023-03-15T10:29:44.894Z',
  availableTo: {},
  editableBy: {},
  protocolMatchingRules: [],
  imageLoadStrategy: 'interleaveCenter',
  displaySetSelectors: {
    mprDisplaySet: {
      seriesMatchingRules: [{
        weight: 1,
        attribute: 'isReconstructable',
        constraint: {
          equals: {
            value: true
          }
        },
        required: true
      }, {
        attribute: 'Modality',
        constraint: {
          equals: {
            value: 'CT'
          }
        },
        required: true
      }]
    }
  },
  stages: [{
    id: 'mpr3Stage',
    name: 'mpr',
    viewportStructure: {
      layoutType: 'grid',
      properties: {
        rows: 2,
        columns: 2
      }
    },
    viewports: [{
      viewportOptions: {
        toolGroupId: 'mpr',
        viewportType: 'volume',
        orientation: 'axial',
        initialImageOptions: {
          preset: 'middle'
        },
        syncGroups: [{
          type: 'voi',
          id: 'mpr',
          source: true,
          target: true
        }]
      },
      displaySets: [{
        id: 'mprDisplaySet'
      }]
    }, {
      viewportOptions: {
        toolGroupId: 'volume3d',
        viewportType: 'volume3d',
        orientation: 'coronal',
        customViewportProps: {
          hideOverlays: true
        }
      },
      displaySets: [{
        id: 'mprDisplaySet',
        options: {
          displayPreset: 'CT-Bone'
        }
      }]
    }, {
      viewportOptions: {
        toolGroupId: 'mpr',
        viewportType: 'volume',
        orientation: 'coronal',
        initialImageOptions: {
          preset: 'middle'
        },
        syncGroups: [{
          type: 'voi',
          id: 'mpr',
          source: true,
          target: true
        }]
      },
      displaySets: [{
        id: 'mprDisplaySet'
      }]
    }, {
      viewportOptions: {
        toolGroupId: 'mpr',
        viewportType: 'volume',
        orientation: 'sagittal',
        initialImageOptions: {
          preset: 'middle'
        },
        syncGroups: [{
          type: 'voi',
          id: 'mpr',
          source: true,
          target: true
        }]
      },
      displaySets: [{
        id: 'mprDisplaySet'
      }]
    }]
  }]
};
function getHangingProtocolModule() {
  return [{
    name: mpr.id,
    protocol: mpr
  }, {
    name: mprAnd3DVolumeViewport.id,
    protocol: mprAnd3DVolumeViewport
  }];
}
/* harmony default export */ const src_getHangingProtocolModule = (getHangingProtocolModule);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/ToolGroupService/ToolGroupService.ts



const ToolGroupService_EVENTS = {
  VIEWPORT_ADDED: 'event::cornerstone::toolgroupservice:viewportadded',
  TOOLGROUP_CREATED: 'event::cornerstone::toolgroupservice:toolgroupcreated'
};
class ToolGroupService {
  constructor(serviceManager) {
    this.serviceManager = void 0;
    this.toolGroupIds = new Set();
    /**
     * Service-specific
     */
    this.listeners = void 0;
    this.EVENTS = void 0;
    const {
      cornerstoneViewportService,
      viewportGridService
    } = serviceManager.services;
    this.cornerstoneViewportService = cornerstoneViewportService;
    this.viewportGridService = viewportGridService;
    this.listeners = {};
    this.EVENTS = ToolGroupService_EVENTS;
    Object.assign(this, src/* pubSubServiceInterface */.KZ);
  }
  onModeExit() {
    this.destroy();
  }

  /**
   * Retrieves a tool group from the ToolGroupManager by tool group ID.
   * If no tool group ID is provided, it retrieves the tool group of the active viewport.
   * @param toolGroupId - Optional ID of the tool group to retrieve.
   * @returns The tool group or undefined if it is not found.
   */
  getToolGroup(toolGroupId) {
    let toolGroupIdToUse = toolGroupId;
    if (!toolGroupIdToUse) {
      // Use the active viewport's tool group if no tool group id is provided
      const enabledElement = getActiveViewportEnabledElement(this.viewportGridService);
      if (!enabledElement) {
        return;
      }
      const {
        renderingEngineId,
        viewportId
      } = enabledElement;
      const toolGroup = dist_esm.ToolGroupManager.getToolGroupForViewport(viewportId, renderingEngineId);
      if (!toolGroup) {
        console.warn('No tool group found for viewportId:', viewportId, 'and renderingEngineId:', renderingEngineId);
        return;
      }
      toolGroupIdToUse = toolGroup.id;
    }
    const toolGroup = dist_esm.ToolGroupManager.getToolGroup(toolGroupIdToUse);
    return toolGroup;
  }
  getToolGroupIds() {
    return Array.from(this.toolGroupIds);
  }
  getToolGroupForViewport(viewportId) {
    const renderingEngine = this.cornerstoneViewportService.getRenderingEngine();
    return dist_esm.ToolGroupManager.getToolGroupForViewport(viewportId, renderingEngine.id);
  }
  getActiveToolForViewport(viewportId) {
    const toolGroup = this.getToolGroupForViewport(viewportId);
    if (!toolGroup) {
      return;
    }
    return toolGroup.getActivePrimaryMouseButtonTool();
  }
  destroy() {
    dist_esm.ToolGroupManager.destroy();
    this.toolGroupIds = new Set();
  }
  destroyToolGroup(toolGroupId) {
    dist_esm.ToolGroupManager.destroyToolGroup(toolGroupId);
    this.toolGroupIds.delete(toolGroupId);
  }
  removeViewportFromToolGroup(viewportId, renderingEngineId, deleteToolGroupIfEmpty) {
    const toolGroup = dist_esm.ToolGroupManager.getToolGroupForViewport(viewportId, renderingEngineId);
    if (!toolGroup) {
      return;
    }
    toolGroup.removeViewports(renderingEngineId, viewportId);
    const viewportIds = toolGroup.getViewportIds();
    if (viewportIds.length === 0 && deleteToolGroupIfEmpty) {
      dist_esm.ToolGroupManager.destroyToolGroup(toolGroup.id);
    }
  }
  addViewportToToolGroup(viewportId, renderingEngineId, toolGroupId) {
    if (!toolGroupId) {
      // If toolGroupId is not provided, add the viewport to all toolGroups
      const toolGroups = dist_esm.ToolGroupManager.getAllToolGroups();
      toolGroups.forEach(toolGroup => {
        toolGroup.addViewport(viewportId, renderingEngineId);
      });
    } else {
      let toolGroup = dist_esm.ToolGroupManager.getToolGroup(toolGroupId);
      if (!toolGroup) {
        toolGroup = this.createToolGroup(toolGroupId);
      }
      toolGroup.addViewport(viewportId, renderingEngineId);
    }
    this._broadcastEvent(ToolGroupService_EVENTS.VIEWPORT_ADDED, {
      viewportId,
      toolGroupId
    });
  }
  createToolGroup(toolGroupId) {
    if (this.getToolGroup(toolGroupId)) {
      throw new Error(`ToolGroup ${toolGroupId} already exists`);
    }

    // if the toolGroup doesn't exist, create it
    const toolGroup = dist_esm.ToolGroupManager.createToolGroup(toolGroupId);
    this.toolGroupIds.add(toolGroupId);
    this._broadcastEvent(ToolGroupService_EVENTS.TOOLGROUP_CREATED, {
      toolGroupId
    });
    return toolGroup;
  }
  addToolsToToolGroup(toolGroupId, tools) {
    let configs = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
    const toolGroup = dist_esm.ToolGroupManager.getToolGroup(toolGroupId);
    // this.changeConfigurationIfNecessary(toolGroup, volumeId);
    this._addTools(toolGroup, tools, configs);
    this._setToolsMode(toolGroup, tools);
  }
  createToolGroupAndAddTools(toolGroupId, tools) {
    const toolGroup = this.createToolGroup(toolGroupId);
    this.addToolsToToolGroup(toolGroupId, tools);
    return toolGroup;
  }

  /**
  private changeConfigurationIfNecessary(toolGroup, volumeUID) {
    // handle specific assignment for volumeUID (e.g., fusion)
    const toolInstances = toolGroup._toolInstances;
    // Object.values(toolInstances).forEach(toolInstance => {
    //   if (toolInstance.configuration) {
    //     toolInstance.configuration.volumeUID = volumeUID;
    //   }
    // });
  }
  */

  /**
   * Get the tool's configuration based on the tool name and tool group id
   * @param toolGroupId - The id of the tool group that the tool instance belongs to.
   * @param toolName - The name of the tool
   * @returns The configuration of the tool.
   */
  getToolConfiguration(toolGroupId, toolName) {
    const toolGroup = dist_esm.ToolGroupManager.getToolGroup(toolGroupId);
    if (!toolGroup) {
      return null;
    }
    const tool = toolGroup.getToolInstance(toolName);
    if (!tool) {
      return null;
    }
    return tool.configuration;
  }

  /**
   * Set the tool instance configuration. This will update the tool instance configuration
   * on the toolGroup
   * @param toolGroupId - The id of the tool group that the tool instance belongs to.
   * @param toolName - The name of the tool
   * @param config - The configuration object that you want to set.
   */
  setToolConfiguration(toolGroupId, toolName, config) {
    const toolGroup = dist_esm.ToolGroupManager.getToolGroup(toolGroupId);
    const toolInstance = toolGroup.getToolInstance(toolName);
    toolInstance.configuration = config;
  }
  _setToolsMode(toolGroup, tools) {
    const {
      active,
      passive,
      enabled,
      disabled
    } = tools;
    if (active) {
      active.forEach(_ref => {
        let {
          toolName,
          bindings
        } = _ref;
        toolGroup.setToolActive(toolName, {
          bindings
        });
      });
    }
    if (passive) {
      passive.forEach(_ref2 => {
        let {
          toolName
        } = _ref2;
        toolGroup.setToolPassive(toolName);
      });
    }
    if (enabled) {
      enabled.forEach(_ref3 => {
        let {
          toolName
        } = _ref3;
        toolGroup.setToolEnabled(toolName);
      });
    }
    if (disabled) {
      disabled.forEach(_ref4 => {
        let {
          toolName
        } = _ref4;
        toolGroup.setToolDisabled(toolName);
      });
    }
  }
  _addTools(toolGroup, tools) {
    const addTools = tools => {
      tools.forEach(_ref5 => {
        let {
          toolName,
          parentTool,
          configuration
        } = _ref5;
        if (parentTool) {
          toolGroup.addToolInstance(toolName, parentTool, {
            ...configuration
          });
        } else {
          toolGroup.addTool(toolName, {
            ...configuration
          });
        }
      });
    };
    if (tools.active) {
      addTools(tools.active);
    }
    if (tools.passive) {
      addTools(tools.passive);
    }
    if (tools.enabled) {
      addTools(tools.enabled);
    }
    if (tools.disabled) {
      addTools(tools.disabled);
    }
  }
}
ToolGroupService.REGISTRATION = {
  name: 'toolGroupService',
  altName: 'ToolGroupService',
  create: _ref6 => {
    let {
      servicesManager
    } = _ref6;
    return new ToolGroupService(servicesManager);
  }
};
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/ToolGroupService/index.js

/* harmony default export */ const services_ToolGroupService = (ToolGroupService);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/SyncGroupService/SyncGroupService.ts


const SyncGroupService_EVENTS = {
  TOOL_GROUP_CREATED: 'event::cornerstone::syncgroupservice:toolgroupcreated'
};

/**
 * @params options - are an optional set of options associated with the first
 * sync group declared.
 */

const POSITION = 'cameraposition';
const VOI = 'voi';
const ZOOMPAN = 'zoompan';
const STACKIMAGE = 'stackimage';
const asSyncGroup = syncGroup => typeof syncGroup === 'string' ? {
  type: syncGroup
} : syncGroup;
class SyncGroupService {
  constructor(serviceManager) {
    this.servicesManager = void 0;
    this.listeners = {};
    this.EVENTS = void 0;
    this.synchronizerCreators = {
      [POSITION]: dist_esm.synchronizers.createCameraPositionSynchronizer,
      [VOI]: dist_esm.synchronizers.createVOISynchronizer,
      [ZOOMPAN]: dist_esm.synchronizers.createZoomPanSynchronizer,
      [STACKIMAGE]: dist_esm.synchronizers.createStackImageSynchronizer
    };
    this.servicesManager = serviceManager;
    this.listeners = {};
    this.EVENTS = SyncGroupService_EVENTS;
    //
    Object.assign(this, src/* pubSubServiceInterface */.KZ);
  }
  _createSynchronizer(type, id, options) {
    const syncCreator = this.synchronizerCreators[type.toLowerCase()];
    if (syncCreator) {
      return syncCreator(id, options);
    } else {
      console.warn('Unknown synchronizer type', type, id);
    }
  }

  /**
   * Creates a synchronizer type.
   * @param type is the type of the synchronizer to create
   * @param creator
   */
  addSynchronizerType(type, creator) {
    this.synchronizerCreators[type.toLowerCase()] = creator;
  }
  _getOrCreateSynchronizer(type, id, options) {
    let synchronizer = dist_esm.SynchronizerManager.getSynchronizer(id);
    if (!synchronizer) {
      synchronizer = this._createSynchronizer(type, id, options);
    }
    return synchronizer;
  }
  addViewportToSyncGroup(viewportId, renderingEngineId, syncGroups) {
    if (!syncGroups) {
      return;
    }
    const syncGroupsArray = Array.isArray(syncGroups) ? syncGroups : [syncGroups];
    syncGroupsArray.forEach(syncGroup => {
      const syncGroupObj = asSyncGroup(syncGroup);
      const {
        type,
        target = true,
        source = true,
        options = {},
        id = type
      } = syncGroupObj;
      const synchronizer = this._getOrCreateSynchronizer(type, id, options);
      synchronizer.setOptions(viewportId, options);
      const viewportInfo = {
        viewportId,
        renderingEngineId
      };
      if (target && source) {
        synchronizer.add(viewportInfo);
        return;
      } else if (source) {
        synchronizer.addSource(viewportInfo);
      } else if (target) {
        synchronizer.addTarget(viewportInfo);
      }
    });
  }
  destroy() {
    dist_esm.SynchronizerManager.destroy();
  }
  removeViewportFromSyncGroup(viewportId, renderingEngineId, syncGroupId) {
    const synchronizers = dist_esm.SynchronizerManager.getAllSynchronizers();
    const filteredSynchronizers = syncGroupId ? synchronizers.filter(s => s.id === syncGroupId) : synchronizers;
    filteredSynchronizers.forEach(synchronizer => {
      if (!synchronizer) {
        return;
      }
      synchronizer.remove({
        viewportId,
        renderingEngineId
      });

      // check if any viewport is left in any of the sync groups, if not, delete that sync group
      const sourceViewports = synchronizer.getSourceViewports();
      const targetViewports = synchronizer.getTargetViewports();
      if (!sourceViewports.length && !targetViewports.length) {
        dist_esm.SynchronizerManager.destroySynchronizer(synchronizer.id);
      }
    });
  }
}
SyncGroupService.REGISTRATION = {
  name: 'syncGroupService',
  altName: 'SyncGroupService',
  create: _ref => {
    let {
      servicesManager
    } = _ref;
    return new SyncGroupService(servicesManager);
  }
};
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/SyncGroupService/index.js

/* harmony default export */ const services_SyncGroupService = (SyncGroupService);
// EXTERNAL MODULE: ../../../node_modules/lodash.clonedeep/index.js
var lodash_clonedeep = __webpack_require__(11677);
var lodash_clonedeep_default = /*#__PURE__*/__webpack_require__.n(lodash_clonedeep);
// EXTERNAL MODULE: ../../../node_modules/lodash.isequal/index.js
var lodash_isequal = __webpack_require__(10311);
var lodash_isequal_default = /*#__PURE__*/__webpack_require__.n(lodash_isequal);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/transitions.ts
/**
 * It is a bell curved function that uses ease in out quadratic for css
 * transition timing function for each side of the curve.
 *
 * @param {number} x - The current time, in the range [0, 1].
 * @param {number} baseline - The baseline value to start from and return to.
 * @returns the value of the transition at time x.
 */
function easeInOutBell(x, baseline) {
  const alpha = 1 - baseline;

  // prettier-ignore
  if (x < 1 / 4) {
    return 4 * Math.pow(2 * x, 3) * alpha + baseline;
  } else if (x < 1 / 2) {
    return (1 - Math.pow(-4 * x + 2, 3) / 2) * alpha + baseline;
  } else if (x < 3 / 4) {
    return (1 - Math.pow(4 * x - 2, 3) / 2) * alpha + baseline;
  } else {
    return -4 * Math.pow(2 * x - 2, 3) * alpha + baseline;
  }
}

/**
 * A reversed bell curved function that starts from 1 and goes to baseline and
 * come back to 1 again. It uses ease in out quadratic for css transition
 * timing function for each side of the curve.
 *
 * @param {number} x - The current time, in the range [0, 1].
 * @param {number} baseline - The baseline value to start from and return to.
 * @returns the value of the transition at time x.
 */
function reverseEaseInOutBell(x, baseline) {
  const y = easeInOutBell(x, baseline);
  return -y + 1 + baseline;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/SegmentationService/RTSTRUCT/mapROIContoursToRTStructData.ts
/**
 * Maps a DICOM RT Struct ROI Contour to a RTStruct data that can be used
 * in Segmentation Service
 *
 * @param structureSet - A DICOM RT Struct ROI Contour
 * @param rtDisplaySetUID - A CornerstoneTools DisplaySet UID
 * @returns An array of object that includes data, id, segmentIndex, color
 * and geometry Id
 */
function mapROIContoursToRTStructData(structureSet, rtDisplaySetUID) {
  return structureSet.ROIContours.map(_ref => {
    let {
      contourPoints,
      ROINumber,
      ROIName,
      colorArray
    } = _ref;
    const data = contourPoints.map(_ref2 => {
      let {
        points,
        ...rest
      } = _ref2;
      const newPoints = points.map(_ref3 => {
        let {
          x,
          y,
          z
        } = _ref3;
        return [x, y, z];
      });
      return {
        ...rest,
        points: newPoints
      };
    });
    const id = ROIName || ROINumber;
    return {
      data,
      id,
      segmentIndex: ROINumber,
      color: colorArray,
      geometryId: `${rtDisplaySetUID}:${id}:segmentIndex-${ROINumber}`
    };
  });
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/SegmentationService/SegmentationService.ts







const {
  COLOR_LUT
} = dist_esm.CONSTANTS;
const LABELMAP = dist_esm.Enums.SegmentationRepresentations.Labelmap;
const CONTOUR = dist_esm.Enums.SegmentationRepresentations.Contour;
const SegmentationService_EVENTS = {
  // fired when the segmentation is updated (e.g. when a segment is added, removed, or modified, locked, visibility changed etc.)
  SEGMENTATION_UPDATED: 'event::segmentation_updated',
  // fired when the segmentation data (e.g., labelmap pixels) is modified
  SEGMENTATION_DATA_MODIFIED: 'event::segmentation_data_modified',
  // fired when the segmentation is added to the cornerstone
  SEGMENTATION_ADDED: 'event::segmentation_added',
  // fired when the segmentation is removed
  SEGMENTATION_REMOVED: 'event::segmentation_removed',
  // fired when the configuration for the segmentation is changed (e.g., brush size, render fill, outline thickness, etc.)
  SEGMENTATION_CONFIGURATION_CHANGED: 'event::segmentation_configuration_changed',
  // fired when the active segment is loaded in SEG or RTSTRUCT
  SEGMENT_LOADING_COMPLETE: 'event::segment_loading_complete',
  // for all segments
  SEGMENTATION_LOADING_COMPLETE: 'event::segmentation_loading_complete'
};
const VALUE_TYPES = {};
const SEGMENT_CONSTANT = {
  opacity: 255,
  isVisible: true,
  isLocked: false
};
const VOLUME_LOADER_SCHEME = 'cornerstoneStreamingImageVolume';
class SegmentationService extends src/* PubSubService */.hC {
  constructor(_ref) {
    var _this;
    let {
      servicesManager
    } = _ref;
    super(SegmentationService_EVENTS);
    _this = this;
    this.segmentations = void 0;
    this.servicesManager = void 0;
    this.highlightIntervalId = null;
    this.EVENTS = SegmentationService_EVENTS;
    this.destroy = () => {
      esm.eventTarget.removeEventListener(dist_esm.Enums.Events.SEGMENTATION_MODIFIED, this._onSegmentationModifiedFromSource);
      esm.eventTarget.removeEventListener(dist_esm.Enums.Events.SEGMENTATION_DATA_MODIFIED, this._onSegmentationDataModified);

      // remove the segmentations from the cornerstone
      Object.keys(this.segmentations).forEach(segmentationId => {
        this._removeSegmentationFromCornerstone(segmentationId);
      });
      this.segmentations = {};
      this.listeners = {};
    };
    this.setSegmentRGBA = (segmentationId, segmentIndex, rgbaColor, toolGroupId) => {
      const segmentation = this.getSegmentation(segmentationId);
      if (segmentation === undefined) {
        throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
      }
      const suppressEvents = true;
      this._setSegmentOpacity(segmentationId, segmentIndex, rgbaColor[3], toolGroupId, suppressEvents);
      this._setSegmentColor(segmentationId, segmentIndex, [rgbaColor[0], rgbaColor[1], rgbaColor[2]], toolGroupId, suppressEvents);
      this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
        segmentation
      });
    };
    // Todo: this should not run on the main thread
    this.calculateCentroids = (segmentationId, segmentIndex) => {
      const segmentation = this.getSegmentation(segmentationId);
      const volume = this.getLabelmapVolume(segmentationId);
      const {
        dimensions,
        imageData
      } = volume;
      const scalarData = volume.getScalarData();
      const [dimX, dimY, numFrames] = dimensions;
      const frameLength = dimX * dimY;
      const segmentIndices = segmentIndex ? [segmentIndex] : segmentation.segments.filter(segment => segment?.segmentIndex).map(segment => segment.segmentIndex);
      const segmentIndicesSet = new Set(segmentIndices);
      const centroids = new Map();
      for (const index of segmentIndicesSet) {
        centroids.set(index, {
          x: 0,
          y: 0,
          z: 0,
          count: 0
        });
      }
      let voxelIndex = 0;
      for (let frame = 0; frame < numFrames; frame++) {
        for (let p = 0; p < frameLength; p++) {
          const segmentIndex = scalarData[voxelIndex++];
          if (segmentIndicesSet.has(segmentIndex)) {
            const centroid = centroids.get(segmentIndex);
            centroid.x += p % dimX;
            centroid.y += p / dimX | 0;
            centroid.z += frame;
            centroid.count++;
          }
        }
      }
      const result = new Map();
      for (const [index, centroid] of centroids) {
        const count = centroid.count;
        const normalizedCentroid = {
          x: centroid.x / count,
          y: centroid.y / count,
          z: centroid.z / count
        };
        normalizedCentroid.world = imageData.indexToWorld([normalizedCentroid.x, normalizedCentroid.y, normalizedCentroid.z]);
        result.set(index, normalizedCentroid);
      }
      this.setCentroids(segmentationId, result);
      return result;
    };
    this.setCentroids = (segmentationId, centroids) => {
      const segmentation = this.getSegmentation(segmentationId);
      const imageData = this.getLabelmapVolume(segmentationId).imageData; // Assuming this method returns imageData

      if (!segmentation.cachedStats) {
        segmentation.cachedStats = {
          segmentCenter: {}
        };
      } else if (!segmentation.cachedStats.segmentCenter) {
        segmentation.cachedStats.segmentCenter = {};
      }
      for (const [segmentIndex, centroid] of centroids) {
        let world = centroid.world;

        // If world coordinates are not provided, calculate them
        if (!world || world.length === 0) {
          world = imageData.indexToWorld(centroid.image);
        }
        segmentation.cachedStats.segmentCenter[segmentIndex] = {
          center: {
            image: centroid.image,
            world: world
          }
        };
      }
      this.addOrUpdateSegmentation(segmentation, true, true);
    };
    this.createSegmentationForDisplaySet = async (displaySetInstanceUID, options) => {
      const {
        displaySetService
      } = this.servicesManager.services;
      const displaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);

      // Todo: we currently only support labelmap for segmentation for a displaySet
      const representationType = LABELMAP;
      const volumeId = this._getVolumeIdForDisplaySet(displaySet);
      const segmentationId = options?.segmentationId ?? `${esm.utilities.uuidv4()}`;

      // Force use of a Uint8Array SharedArrayBuffer for the segmentation to save space and so
      // it is easily compressible in worker thread.
      await esm.volumeLoader.createAndCacheDerivedVolume(volumeId, {
        volumeId: segmentationId,
        targetBuffer: {
          type: 'Uint8Array',
          sharedArrayBuffer: window.SharedArrayBuffer
        }
      });
      const defaultScheme = this._getDefaultSegmentationScheme();
      const segmentation = {
        ...defaultScheme,
        id: segmentationId,
        displaySetInstanceUID,
        label: options?.label,
        // We should set it as active by default, as it created for display
        isActive: true,
        type: representationType,
        FrameOfReferenceUID: options?.FrameOfReferenceUID || displaySet.instances?.[0]?.FrameOfReferenceUID,
        representationData: {
          LABELMAP: {
            volumeId: segmentationId,
            referencedVolumeId: volumeId // Todo: this is so ugly
          }
        }
      };

      this.addOrUpdateSegmentation(segmentation);
      return segmentationId;
    };
    /**
     * Toggles the visibility of a segmentation in the state, and broadcasts the event.
     * Note: this method does not update the segmentation state in the source. It only
     * updates the state, and there should be separate listeners for that.
     * @param ids segmentation ids
     */
    this.toggleSegmentationVisibility = segmentationId => {
      this._toggleSegmentationVisibility(segmentationId, false);
    };
    this.addSegmentationRepresentationToToolGroup = async function (toolGroupId, segmentationId) {
      let hydrateSegmentation = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : false;
      let representationType = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : dist_esm.Enums.SegmentationRepresentations.Labelmap;
      let suppressEvents = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : false;
      const segmentation = _this.getSegmentation(segmentationId);
      if (!segmentation) {
        throw new Error(`Segmentation with segmentationId ${segmentationId} not found.`);
      }
      if (hydrateSegmentation) {
        // hydrate the segmentation if it's not hydrated yet
        segmentation.hydrated = true;
      }
      const {
        colorLUTIndex
      } = segmentation;

      // Based on the segmentationId, set the colorLUTIndex.
      const segmentationRepresentationUIDs = await dist_esm.segmentation.addSegmentationRepresentations(toolGroupId, [{
        segmentationId,
        type: representationType
      }]);

      // set the latest segmentation representation as active one
      _this._setActiveSegmentationForToolGroup(segmentationId, toolGroupId, segmentationRepresentationUIDs[0]);
      dist_esm.segmentation.config.color.setColorLUT(toolGroupId, segmentationRepresentationUIDs[0], colorLUTIndex);

      // add the segmentation segments properly
      for (const segment of segmentation.segments) {
        if (segment === null || segment === undefined) {
          continue;
        }
        const {
          segmentIndex,
          color,
          isLocked,
          isVisible: visibility,
          opacity
        } = segment;
        const suppressEvents = true;
        if (color !== undefined) {
          _this._setSegmentColor(segmentationId, segmentIndex, color, toolGroupId, suppressEvents);
        }
        if (opacity !== undefined) {
          _this._setSegmentOpacity(segmentationId, segmentIndex, opacity, toolGroupId, suppressEvents);
        }
        if (visibility !== undefined) {
          _this._setSegmentVisibility(segmentationId, segmentIndex, visibility, toolGroupId, suppressEvents);
        }
        if (isLocked) {
          _this._setSegmentLocked(segmentationId, segmentIndex, isLocked, suppressEvents);
        }
      }
      if (!suppressEvents) {
        _this._broadcastEvent(_this.EVENTS.SEGMENTATION_UPDATED, {
          segmentation
        });
      }
    };
    this.setSegmentRGBAColor = (segmentationId, segmentIndex, rgbaColor, toolGroupId) => {
      const segmentation = this.getSegmentation(segmentationId);
      if (segmentation === undefined) {
        throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
      }
      this._setSegmentOpacity(segmentationId, segmentIndex, rgbaColor[3], toolGroupId,
      // toolGroupId
      true);
      this._setSegmentColor(segmentationId, segmentIndex, [rgbaColor[0], rgbaColor[1], rgbaColor[2]], toolGroupId,
      // toolGroupId
      true);
      this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
        segmentation
      });
    };
    this.getToolGroupIdsWithSegmentation = segmentationId => {
      const toolGroupIds = dist_esm.segmentation.state.getToolGroupIdsWithSegmentation(segmentationId);
      return toolGroupIds;
    };
    this.hydrateSegmentation = function (segmentationId) {
      let suppressEvents = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : false;
      const segmentation = _this.getSegmentation(segmentationId);
      if (!segmentation) {
        throw new Error(`Segmentation with segmentationId ${segmentationId} not found.`);
      }
      segmentation.hydrated = true;

      // Not all segmentations have dipslaysets, some of them are derived in the client
      _this._setDisplaySetIsHydrated(segmentationId, true);
      if (!suppressEvents) {
        _this._broadcastEvent(_this.EVENTS.SEGMENTATION_UPDATED, {
          segmentation
        });
      }
    };
    this.getConfiguration = toolGroupId => {
      toolGroupId = toolGroupId ?? this._getApplicableToolGroupId();
      const brushSize = 1;
      // const brushSize = cstUtils.segmentation.getBrushSizeForToolGroup(
      //   toolGroupId
      // );

      const brushThresholdGate = 1;
      // const brushThresholdGate = cstUtils.segmentation.getBrushThresholdForToolGroup(
      //   toolGroupId
      // );

      const segmentationRepresentations = this.getSegmentationRepresentationsForToolGroup(toolGroupId);
      const typeToUse = segmentationRepresentations?.[0]?.type || LABELMAP;
      const config = dist_esm.segmentation.config.getGlobalConfig();
      const {
        renderInactiveSegmentations
      } = config;
      const representation = config.representations[typeToUse];
      const {
        renderOutline,
        outlineWidthActive,
        renderFill,
        fillAlpha,
        fillAlphaInactive,
        outlineOpacity,
        outlineOpacityInactive
      } = representation;
      return {
        brushSize,
        brushThresholdGate,
        fillAlpha,
        fillAlphaInactive,
        outlineWidthActive,
        renderFill,
        renderInactiveSegmentations,
        renderOutline,
        outlineOpacity,
        outlineOpacityInactive
      };
    };
    this.setConfiguration = configuration => {
      const {
        brushSize,
        brushThresholdGate,
        fillAlpha,
        fillAlphaInactive,
        outlineWidthActive,
        outlineOpacity,
        renderFill,
        renderInactiveSegmentations,
        renderOutline
      } = configuration;
      const setConfigValueIfDefined = function (key, value) {
        let transformFn = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : null;
        if (value !== undefined) {
          const transformedValue = transformFn ? transformFn(value) : value;
          _this._setSegmentationConfig(key, transformedValue);
        }
      };
      setConfigValueIfDefined('renderOutline', renderOutline);
      setConfigValueIfDefined('outlineWidthActive', outlineWidthActive);
      setConfigValueIfDefined('outlineOpacity', outlineOpacity, v => v / 100);
      setConfigValueIfDefined('fillAlpha', fillAlpha, v => v / 100);
      setConfigValueIfDefined('renderFill', renderFill);
      setConfigValueIfDefined('fillAlphaInactive', fillAlphaInactive, v => v / 100);
      setConfigValueIfDefined('outlineOpacityInactive', fillAlphaInactive, v => Math.max(0.75, v / 100));
      if (renderInactiveSegmentations !== undefined) {
        const config = dist_esm.segmentation.config.getGlobalConfig();
        config.renderInactiveSegmentations = renderInactiveSegmentations;
        dist_esm.segmentation.config.setGlobalConfig(config);
      }

      // if (brushSize !== undefined) {
      //   const { toolGroupService } = this.servicesManager.services;

      //   const toolGroupIds = toolGroupService.getToolGroupIds();

      //   toolGroupIds.forEach(toolGroupId => {
      //     cstUtils.segmentation.setBrushSizeForToolGroup(toolGroupId, brushSize);
      //   });
      // }

      // if (brushThresholdGate !== undefined) {
      //   const { toolGroupService } = this.servicesManager.services;

      //   const toolGroupIds = toolGroupService.getFirstToolGroupIds();

      //   toolGroupIds.forEach(toolGroupId => {
      //     cstUtils.segmentation.setBrushThresholdForToolGroup(
      //       toolGroupId,
      //       brushThresholdGate
      //     );
      //   });
      // }

      this._broadcastEvent(this.EVENTS.SEGMENTATION_CONFIGURATION_CHANGED, this.getConfiguration());
    };
    this.getLabelmapVolume = segmentationId => {
      return esm.cache.getVolume(segmentationId);
    };
    this.getSegmentationRepresentationsForToolGroup = toolGroupId => {
      return dist_esm.segmentation.state.getSegmentationRepresentations(toolGroupId);
    };
    this._toggleSegmentationVisibility = function (segmentationId) {
      let suppressEvents = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : false;
      const segmentation = _this.segmentations[segmentationId];
      if (!segmentation) {
        throw new Error(`Segmentation with segmentationId ${segmentationId} not found.`);
      }
      segmentation.isVisible = !segmentation.isVisible;
      _this._updateCornerstoneSegmentationVisibility(segmentationId);
      if (suppressEvents === false) {
        _this._broadcastEvent(_this.EVENTS.SEGMENTATION_UPDATED, {
          segmentation
        });
      }
    };
    this._setSegmentColor = function (segmentationId, segmentIndex, color, toolGroupId) {
      let suppressEvents = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : false;
      const segmentation = _this.getSegmentation(segmentationId);
      if (segmentation === undefined) {
        throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
      }
      const segmentInfo = _this._getSegmentInfo(segmentation, segmentIndex);
      if (segmentInfo === undefined) {
        throw new Error(`Segment ${segmentIndex} not yet added to segmentation: ${segmentationId}`);
      }
      toolGroupId = toolGroupId ?? _this._getApplicableToolGroupId();
      const segmentationRepresentation = _this._getSegmentationRepresentation(segmentationId, toolGroupId);
      if (!segmentationRepresentation) {
        throw new Error('Must add representation to toolgroup before setting segments');
      }
      const {
        segmentationRepresentationUID
      } = segmentationRepresentation;
      const rgbaColor = dist_esm.segmentation.config.color.getColorForSegmentIndex(toolGroupId, segmentationRepresentationUID, segmentIndex);
      dist_esm.segmentation.config.color.setColorForSegmentIndex(toolGroupId, segmentationRepresentationUID, segmentIndex, [...color, rgbaColor[3]]);
      segmentInfo.color = color;
      if (suppressEvents === false) {
        _this._broadcastEvent(_this.EVENTS.SEGMENTATION_UPDATED, {
          segmentation
        });
      }
    };
    this._setSegmentOpacity = function (segmentationId, segmentIndex, opacity, toolGroupId) {
      let suppressEvents = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : false;
      const segmentation = _this.getSegmentation(segmentationId);
      if (segmentation === undefined) {
        throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
      }
      const segmentInfo = _this._getSegmentInfo(segmentation, segmentIndex);
      if (segmentInfo === undefined) {
        throw new Error(`Segment ${segmentIndex} not yet added to segmentation: ${segmentationId}`);
      }
      toolGroupId = toolGroupId ?? _this._getApplicableToolGroupId();
      const segmentationRepresentation = _this._getSegmentationRepresentation(segmentationId, toolGroupId);
      if (!segmentationRepresentation) {
        throw new Error('Must add representation to toolgroup before setting segments');
      }
      const {
        segmentationRepresentationUID
      } = segmentationRepresentation;
      const rgbaColor = dist_esm.segmentation.config.color.getColorForSegmentIndex(toolGroupId, segmentationRepresentationUID, segmentIndex);
      dist_esm.segmentation.config.color.setColorForSegmentIndex(toolGroupId, segmentationRepresentationUID, segmentIndex, [rgbaColor[0], rgbaColor[1], rgbaColor[2], opacity]);
      segmentInfo.opacity = opacity;
      if (suppressEvents === false) {
        _this._broadcastEvent(_this.EVENTS.SEGMENTATION_UPDATED, {
          segmentation
        });
      }
    };
    this._setSegmentationConfig = (property, value) => {
      // Todo: currently we only support global config, and we get the type
      // from the first segmentation
      const typeToUse = this.getSegmentations()[0].type;
      const {
        cornerstoneViewportService
      } = this.servicesManager.services;
      const config = dist_esm.segmentation.config.getGlobalConfig();
      config.representations[typeToUse][property] = value;

      // Todo: add non global (representation specific config as well)
      dist_esm.segmentation.config.setGlobalConfig(config);
      const renderingEngine = cornerstoneViewportService.getRenderingEngine();
      const viewportIds = cornerstoneViewportService.getViewportIds();
      renderingEngine.renderViewports(viewportIds);
    };
    this._onSegmentationDataModified = evt => {
      const {
        segmentationId
      } = evt.detail;
      const segmentation = this.getSegmentation(segmentationId);
      if (segmentation === undefined) {
        // Part of add operation, not update operation, exit early.
        return;
      }
      this._broadcastEvent(this.EVENTS.SEGMENTATION_DATA_MODIFIED, {
        segmentation
      });
    };
    this._onSegmentationModifiedFromSource = evt => {
      const {
        segmentationId
      } = evt.detail;
      const segmentation = this.segmentations[segmentationId];
      if (segmentation === undefined) {
        // Part of add operation, not update operation, exit early.
        return;
      }
      const segmentationState = dist_esm.segmentation.state.getSegmentation(segmentationId);
      if (!segmentationState) {
        return;
      }
      const {
        activeSegmentIndex,
        cachedStats,
        segmentsLocked,
        label,
        type
      } = segmentationState;
      if (![LABELMAP, CONTOUR].includes(type)) {
        throw new Error(`Unsupported segmentation type: ${type}. Only ${LABELMAP} and ${CONTOUR} are supported.`);
      }
      const representationData = segmentationState.representationData[type];

      // TODO: handle other representations when available in cornerstone3D
      const segmentationSchema = {
        ...segmentation,
        activeSegmentIndex,
        cachedStats,
        displayText: [],
        id: segmentationId,
        label,
        segmentsLocked,
        type,
        representationData: {
          [type]: {
            ...representationData
          }
        }
      };
      try {
        this.addOrUpdateSegmentation(segmentationSchema);
      } catch (error) {
        console.warn(`Failed to add/update segmentation ${segmentationId}`, error);
      }
    };
    this._updateCornerstoneSegmentationVisibility = segmentationId => {
      const segmentationState = dist_esm.segmentation.state;
      const toolGroupIds = segmentationState.getToolGroupIdsWithSegmentation(segmentationId);
      toolGroupIds.forEach(toolGroupId => {
        const segmentationRepresentations = dist_esm.segmentation.state.getSegmentationRepresentations(toolGroupId);
        if (segmentationRepresentations.length === 0) {
          return;
        }

        // Todo: this finds the first segmentation representation that matches the segmentationId
        // If there are two labelmap representations from the same segmentation, this will not work
        const representation = segmentationRepresentations.find(representation => representation.segmentationId === segmentationId);
        const {
          segmentsHidden
        } = representation;
        const currentVisibility = segmentsHidden.size === 0 ? true : false;
        const newVisibility = !currentVisibility;
        dist_esm.segmentation.config.visibility.setSegmentationVisibility(toolGroupId, representation.segmentationRepresentationUID, newVisibility);

        // update segments visibility
        const {
          segmentation
        } = this._getSegmentationInfo(segmentationId, toolGroupId);
        const segments = segmentation.segments.filter(Boolean);
        segments.forEach(segment => {
          segment.isVisible = newVisibility;
        });
      });
    };
    this._getApplicableToolGroupId = () => {
      const {
        toolGroupService,
        viewportGridService,
        cornerstoneViewportService
      } = this.servicesManager.services;
      const viewportInfo = cornerstoneViewportService.getViewportInfo(viewportGridService.getActiveViewportId());
      if (!viewportInfo) {
        const toolGroupIds = toolGroupService.getToolGroupIds();
        return toolGroupIds[0];
      }
      return viewportInfo.getToolGroupId();
    };
    this.getNextColorLUTIndex = () => {
      let i = 0;
      while (true) {
        if (dist_esm.segmentation.state.getColorLUT(i) === undefined) {
          return i;
        }
        i++;
      }
    };
    /**
     * Converts object of objects to array.
     *
     * @return {Array} Array of objects
     */
    this.arrayOfObjects = obj => {
      return Object.entries(obj).map(e => ({
        [e[0]]: e[1]
      }));
    };
    this.segmentations = {};
    this.servicesManager = servicesManager;
    this._initSegmentationService();
  }
  /**
   * Adds a new segment to the specified segmentation.
   * @param segmentationId - The ID of the segmentation to add the segment to.
   * @param config - An object containing the configuration options for the new segment.
   *   - segmentIndex: (optional) The index of the segment to add. If not provided, the next available index will be used.
   *   - toolGroupId: (optional) The ID of the tool group to associate the new segment with. If not provided, the first available tool group will be used.
   *   - properties: (optional) An object containing the properties of the new segment.
   *     - label: (optional) The label of the new segment. If not provided, a default label will be used.
   *     - color: (optional) The color of the new segment in RGB format. If not provided, a default color will be used.
   *     - opacity: (optional) The opacity of the new segment. If not provided, a default opacity will be used.
   *     - visibility: (optional) Whether the new segment should be visible. If not provided, the segment will be visible by default.
   *     - isLocked: (optional) Whether the new segment should be locked for editing. If not provided, the segment will not be locked by default.
   *     - active: (optional) Whether the new segment should be the active segment to be edited. If not provided, the segment will not be active by default.
   */
  addSegment(segmentationId) {
    let config = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
    if (config?.segmentIndex === 0) {
      throw new Error('Segment index 0 is reserved for "no label"');
    }
    const toolGroupId = config.toolGroupId ?? this._getApplicableToolGroupId();
    const {
      segmentationRepresentationUID,
      segmentation
    } = this._getSegmentationInfo(segmentationId, toolGroupId);
    let segmentIndex = config.segmentIndex;
    if (!segmentIndex) {
      // grab the next available segment index
      segmentIndex = segmentation.segments.length === 0 ? 1 : segmentation.segments.length;
    }
    if (this._getSegmentInfo(segmentation, segmentIndex)) {
      throw new Error(`Segment ${segmentIndex} already exists`);
    }
    const rgbaColor = dist_esm.segmentation.config.color.getColorForSegmentIndex(toolGroupId, segmentationRepresentationUID, segmentIndex);
    segmentation.segments[segmentIndex] = {
      label: config.properties?.label ?? `Segment ${segmentIndex}`,
      segmentIndex: segmentIndex,
      color: [rgbaColor[0], rgbaColor[1], rgbaColor[2]],
      opacity: rgbaColor[3],
      isVisible: true,
      isLocked: false
    };
    segmentation.segmentCount++;

    // make the newly added segment the active segment
    this._setActiveSegment(segmentationId, segmentIndex);
    const suppressEvents = true;
    if (config.properties !== undefined) {
      const {
        color: newColor,
        opacity,
        isLocked,
        visibility,
        active
      } = config.properties;
      if (newColor !== undefined) {
        this._setSegmentColor(segmentationId, segmentIndex, newColor, toolGroupId, suppressEvents);
      }
      if (opacity !== undefined) {
        this._setSegmentOpacity(segmentationId, segmentIndex, opacity, toolGroupId, suppressEvents);
      }
      if (visibility !== undefined) {
        this._setSegmentVisibility(segmentationId, segmentIndex, visibility, toolGroupId, suppressEvents);
      }
      if (active === true) {
        this._setActiveSegment(segmentationId, segmentIndex, suppressEvents);
      }
      if (isLocked !== undefined) {
        this._setSegmentLocked(segmentationId, segmentIndex, isLocked, suppressEvents);
      }
    }
    if (segmentation.activeSegmentIndex === null) {
      this._setActiveSegment(segmentationId, segmentIndex, suppressEvents);
    }

    // Todo: this includes non-hydrated segmentations which might not be
    // persisted in the store
    this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
      segmentation
    });
  }
  removeSegment(segmentationId, segmentIndex) {
    const segmentation = this.getSegmentation(segmentationId);
    if (segmentation === undefined) {
      throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
    }
    if (segmentIndex === 0) {
      throw new Error('Segment index 0 is reserved for "no label"');
    }
    if (!this._getSegmentInfo(segmentation, segmentIndex)) {
      return;
    }
    segmentation.segmentCount--;
    segmentation.segments[segmentIndex] = null;

    // Get volume and delete the labels
    // Todo: handle other segmentations other than labelmap
    const labelmapVolume = this.getLabelmapVolume(segmentationId);
    const {
      dimensions
    } = labelmapVolume;
    const scalarData = labelmapVolume.getScalarData();

    // Set all values of this segment to zero and get which frames have been edited.
    const frameLength = dimensions[0] * dimensions[1];
    const numFrames = dimensions[2];
    let voxelIndex = 0;
    const modifiedFrames = new Set();
    for (let frame = 0; frame < numFrames; frame++) {
      for (let p = 0; p < frameLength; p++) {
        if (scalarData[voxelIndex] === segmentIndex) {
          scalarData[voxelIndex] = 0;
          modifiedFrames.add(frame);
        }
        voxelIndex++;
      }
    }
    const modifiedFramesArray = Array.from(modifiedFrames);

    // Trigger texture update of modified segmentation frames.
    dist_esm.segmentation.triggerSegmentationEvents.triggerSegmentationDataModified(segmentationId, modifiedFramesArray);
    if (segmentation.activeSegmentIndex === segmentIndex) {
      const segmentIndices = Object.keys(segmentation.segments);
      const newActiveSegmentIndex = segmentIndices.length ? Number(segmentIndices[0]) : 1;
      this._setActiveSegment(segmentationId, newActiveSegmentIndex, true);
    }
    this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
      segmentation
    });
  }
  setSegmentVisibility(segmentationId, segmentIndex, isVisible, toolGroupId) {
    let suppressEvents = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : false;
    this._setSegmentVisibility(segmentationId, segmentIndex, isVisible, toolGroupId, suppressEvents);
  }
  setSegmentLocked(segmentationId, segmentIndex, isLocked) {
    const suppressEvents = false;
    this._setSegmentLocked(segmentationId, segmentIndex, isLocked, suppressEvents);
  }

  /**
   * Toggles the locked state of a segment in a segmentation.
   * @param segmentationId - The ID of the segmentation.
   * @param segmentIndex - The index of the segment to toggle.
   */
  toggleSegmentLocked(segmentationId, segmentIndex) {
    const segmentation = this.getSegmentation(segmentationId);
    const segment = this._getSegmentInfo(segmentation, segmentIndex);
    const isLocked = !segment.isLocked;
    this._setSegmentLocked(segmentationId, segmentIndex, isLocked);
  }
  setSegmentColor(segmentationId, segmentIndex, color, toolGroupId) {
    this._setSegmentColor(segmentationId, segmentIndex, color, toolGroupId);
  }
  setSegmentOpacity(segmentationId, segmentIndex, opacity, toolGroupId) {
    this._setSegmentOpacity(segmentationId, segmentIndex, opacity, toolGroupId);
  }
  setActiveSegmentationForToolGroup(segmentationId, toolGroupId) {
    toolGroupId = toolGroupId ?? this._getApplicableToolGroupId();
    const suppressEvents = false;
    this._setActiveSegmentationForToolGroup(segmentationId, toolGroupId, suppressEvents);
  }
  setActiveSegment(segmentationId, segmentIndex) {
    this._setActiveSegment(segmentationId, segmentIndex, false);
  }

  /**
   * Get all segmentations.
   *
   * * @param filterNonHydratedSegmentations - If true, only return hydrated segmentations
   * hydrated segmentations are those that have been loaded and persisted
   * in the state, but non hydrated segmentations are those that are
   * only created for the SEG displayset (SEG viewport) and the user might not
   * have loaded them yet fully.
   *
    * @return Array of segmentations
   */
  getSegmentations() {
    let filterNonHydratedSegmentations = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : true;
    const segmentations = this._getSegmentations();
    return segmentations && segmentations.filter(segmentation => {
      return !filterNonHydratedSegmentations || segmentation.hydrated;
    });
  }
  _getSegmentations() {
    const segmentations = this.arrayOfObjects(this.segmentations);
    return segmentations && segmentations.map(m => this.segmentations[Object.keys(m)[0]]);
  }
  getActiveSegmentation() {
    const segmentations = this.getSegmentations();
    return segmentations.find(segmentation => segmentation.isActive);
  }
  getActiveSegment() {
    const activeSegmentation = this.getActiveSegmentation();
    const {
      activeSegmentIndex,
      segments
    } = activeSegmentation;
    if (activeSegmentIndex === null) {
      return;
    }
    return segments[activeSegmentIndex];
  }

  /**
   * Get specific segmentation by its id.
   *
   * @param segmentationId If of the segmentation
   * @return segmentation instance
   */
  getSegmentation(segmentationId) {
    return this.segmentations[segmentationId];
  }
  addOrUpdateSegmentation(segmentation) {
    let suppressEvents = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : false;
    let notYetUpdatedAtSource = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : false;
    const {
      id: segmentationId
    } = segmentation;
    let cachedSegmentation = this.segmentations[segmentationId];
    if (cachedSegmentation) {
      // Update the segmentation (mostly for assigning metadata/labels)
      Object.assign(cachedSegmentation, segmentation);
      this._updateCornerstoneSegmentations({
        segmentationId,
        notYetUpdatedAtSource
      });
      if (!suppressEvents) {
        this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
          segmentation: cachedSegmentation
        });
      }
      return segmentationId;
    }
    const representationType = segmentation.type;
    const representationData = segmentation.representationData[representationType];
    dist_esm.segmentation.addSegmentations([{
      segmentationId,
      representation: {
        type: representationType,
        data: {
          ...representationData
        }
      }
    }]);

    // if first segmentation, we can use the default colorLUT, otherwise
    // we need to generate a new one and use a new colorLUT
    const colorLUTIndex = 0;
    if (Object.keys(this.segmentations).length !== 0) {
      const newColorLUT = this.generateNewColorLUT();
      const colorLUTIndex = this.getNextColorLUTIndex();
      dist_esm.segmentation.config.color.addColorLUT(newColorLUT, colorLUTIndex);
    }
    this.segmentations[segmentationId] = {
      ...segmentation,
      label: segmentation.label || '',
      segments: segmentation.segments || [null],
      activeSegmentIndex: segmentation.activeSegmentIndex ?? null,
      segmentCount: segmentation.segmentCount ?? 0,
      isActive: false,
      isVisible: true,
      colorLUTIndex
    };
    cachedSegmentation = this.segmentations[segmentationId];
    this._updateCornerstoneSegmentations({
      segmentationId,
      notYetUpdatedAtSource: true
    });
    if (!suppressEvents) {
      this._broadcastEvent(this.EVENTS.SEGMENTATION_ADDED, {
        segmentation: cachedSegmentation
      });
    }
    return cachedSegmentation.id;
  }
  async createSegmentationForSEGDisplaySet(segDisplaySet, segmentationId) {
    let suppressEvents = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : false;
    // Todo: we only support creating labelmap for SEG displaySets for now
    const representationType = LABELMAP;
    segmentationId = segmentationId ?? segDisplaySet.displaySetInstanceUID;
    const defaultScheme = this._getDefaultSegmentationScheme();
    const segmentation = {
      ...defaultScheme,
      id: segmentationId,
      displaySetInstanceUID: segDisplaySet.displaySetInstanceUID,
      type: representationType,
      label: segDisplaySet.SeriesDescription,
      representationData: {
        [LABELMAP]: {
          volumeId: segmentationId,
          referencedVolumeId: segDisplaySet.referencedVolumeId
        }
      }
    };
    const labelmap = this.getLabelmapVolume(segmentationId);
    const cachedSegmentation = this.getSegmentation(segmentationId);
    if (labelmap && cachedSegmentation) {
      // if the labelmap with the same segmentationId already exists, we can
      // just assume that the segmentation is already created and move on with
      // updating the state
      return this.addOrUpdateSegmentation(Object.assign(segmentation, cachedSegmentation), suppressEvents);
    }
    const {
      labelmapBufferArray,
      referencedVolumeId
    } = segDisplaySet;
    if (!labelmapBufferArray || !referencedVolumeId) {
      throw new Error('No labelmapBufferArray or referencedVolumeId found for the SEG displaySet');
    }

    // if the labelmap doesn't exist, we need to create it first from the
    // DICOM SEG displaySet data
    const referencedVolume = esm.cache.getVolume(referencedVolumeId);
    if (!referencedVolume) {
      throw new Error(`No volume found for referencedVolumeId: ${referencedVolumeId}`);
    }

    // Force use of a Uint8Array SharedArrayBuffer for the segmentation to save space and so
    // it is easily compressible in worker thread.
    const derivedVolume = await esm.volumeLoader.createAndCacheDerivedVolume(referencedVolumeId, {
      volumeId: segmentationId,
      targetBuffer: {
        type: 'Uint8Array',
        sharedArrayBuffer: window.SharedArrayBuffer
      }
    });
    const derivedVolumeScalarData = derivedVolume.getScalarData();
    const segmentsInfo = segDisplaySet.segMetadata.data;
    derivedVolumeScalarData.set(new Uint8Array(labelmapBufferArray[0]));
    segmentation.segments = segmentsInfo.map((segmentInfo, segmentIndex) => {
      if (segmentIndex === 0) {
        return;
      }
      const {
        SegmentedPropertyCategoryCodeSequence,
        SegmentNumber,
        SegmentLabel,
        SegmentAlgorithmType,
        SegmentAlgorithmName,
        SegmentedPropertyTypeCodeSequence,
        rgba
      } = segmentInfo;
      const {
        x,
        y,
        z
      } = segDisplaySet.centroids.get(segmentIndex);
      const centerWorld = derivedVolume.imageData.indexToWorld([x, y, z]);
      segmentation.cachedStats = {
        ...segmentation.cachedStats,
        segmentCenter: {
          ...segmentation.cachedStats.segmentCenter,
          [segmentIndex]: {
            center: {
              image: [x, y, z],
              world: centerWorld
            },
            modifiedTime: segDisplaySet.SeriesDate
          }
        }
      };
      return {
        label: SegmentLabel || `Segment ${SegmentNumber}`,
        segmentIndex: Number(SegmentNumber),
        category: SegmentedPropertyCategoryCodeSequence ? SegmentedPropertyCategoryCodeSequence.CodeMeaning : '',
        type: SegmentedPropertyTypeCodeSequence ? SegmentedPropertyTypeCodeSequence.CodeMeaning : '',
        algorithmType: SegmentAlgorithmType,
        algorithmName: SegmentAlgorithmName,
        color: rgba,
        opacity: 255,
        isVisible: true,
        isLocked: false
      };
    });
    segmentation.segmentCount = segmentsInfo.length - 1;
    segDisplaySet.isLoaded = true;
    this._broadcastEvent(SegmentationService_EVENTS.SEGMENTATION_LOADING_COMPLETE, {
      segmentationId,
      segDisplaySet
    });
    return this.addOrUpdateSegmentation(segmentation, suppressEvents);
  }
  async createSegmentationForRTDisplaySet(rtDisplaySet, segmentationId) {
    let suppressEvents = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : false;
    // Todo: we currently only have support for contour representation for initial
    // RT display
    const representationType = CONTOUR;
    segmentationId = segmentationId ?? rtDisplaySet.displaySetInstanceUID;
    const {
      structureSet
    } = rtDisplaySet;
    if (!structureSet) {
      throw new Error('To create the contours from RT displaySet, the displaySet should be loaded first, you can perform rtDisplaySet.load() before calling this method.');
    }
    const defaultScheme = this._getDefaultSegmentationScheme();
    const rtDisplaySetUID = rtDisplaySet.displaySetInstanceUID;
    const allRTStructData = mapROIContoursToRTStructData(structureSet, rtDisplaySetUID);

    // sort by segmentIndex
    allRTStructData.sort((a, b) => a.segmentIndex - b.segmentIndex);
    const geometryIds = allRTStructData.map(_ref2 => {
      let {
        geometryId
      } = _ref2;
      return geometryId;
    });
    const segmentation = {
      ...defaultScheme,
      id: segmentationId,
      displaySetInstanceUID: rtDisplaySetUID,
      type: representationType,
      label: rtDisplaySet.SeriesDescription,
      representationData: {
        [CONTOUR]: {
          geometryIds
        }
      }
    };
    const cachedSegmentation = this.getSegmentation(segmentationId);
    if (cachedSegmentation) {
      // if the labelmap with the same segmentationId already exists, we can
      // just assume that the segmentation is already created and move on with
      // updating the state
      return this.addOrUpdateSegmentation(Object.assign(segmentation, cachedSegmentation), suppressEvents);
    }
    if (!structureSet.ROIContours?.length) {
      throw new Error('The structureSet does not contain any ROIContours. Please ensure the structureSet is loaded first.');
    }
    const segmentsCachedStats = {};
    const initializeContour = async rtStructData => {
      const {
        data,
        id,
        color,
        segmentIndex,
        geometryId
      } = rtStructData;

      // catch error instead of failing to allow loading to continue
      try {
        const geometry = await esm.geometryLoader.createAndCacheGeometry(geometryId, {
          geometryData: {
            data,
            id,
            color,
            frameOfReferenceUID: structureSet.frameOfReferenceUID,
            segmentIndex
          },
          type: esm.Enums.GeometryType.CONTOUR
        });
        const contourSet = geometry.data;
        const centroid = contourSet.getCentroid();
        segmentsCachedStats[segmentIndex] = {
          center: {
            world: centroid
          },
          modifiedTime: rtDisplaySet.SeriesDate // we use the SeriesDate as the modifiedTime since this is the first time we are creating the segmentation
        };

        segmentation.segments[segmentIndex] = {
          label: id,
          segmentIndex,
          color,
          ...SEGMENT_CONSTANT
        };
        const numInitialized = Object.keys(segmentsCachedStats).length;

        // Calculate percentage completed
        const percentComplete = Math.round(numInitialized / allRTStructData.length * 100);
        this._broadcastEvent(SegmentationService_EVENTS.SEGMENT_LOADING_COMPLETE, {
          percentComplete,
          // Note: this is not the geometryIds length since there might be
          // some missing ROINumbers
          numSegments: allRTStructData.length
        });
      } catch (e) {
        console.warn(e);
      }
    };
    const promiseArray = [];
    for (let i = 0; i < allRTStructData.length; i++) {
      const promise = new Promise((resolve, reject) => {
        setTimeout(() => {
          initializeContour(allRTStructData[i]).then(() => {
            resolve();
          });
        }, 0);
      });
      promiseArray.push(promise);
    }
    await Promise.all(promiseArray);
    segmentation.segmentCount = allRTStructData.length;
    rtDisplaySet.isLoaded = true;
    segmentation.cachedStats = {
      ...segmentation.cachedStats,
      segmentCenter: {
        ...segmentation.cachedStats.segmentCenter,
        ...segmentsCachedStats
      }
    };
    this._broadcastEvent(SegmentationService_EVENTS.SEGMENTATION_LOADING_COMPLETE, {
      segmentationId,
      rtDisplaySet
    });
    return this.addOrUpdateSegmentation(segmentation, suppressEvents);
  }
  jumpToSegmentCenter(segmentationId, segmentIndex, toolGroupId) {
    let highlightAlpha = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : 0.9;
    let highlightSegment = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : true;
    let animationLength = arguments.length > 5 && arguments[5] !== undefined ? arguments[5] : 750;
    let highlightHideOthers = arguments.length > 6 && arguments[6] !== undefined ? arguments[6] : false;
    let highlightFunctionType = arguments.length > 7 && arguments[7] !== undefined ? arguments[7] : 'ease-in-out';
    const {
      toolGroupService
    } = this.servicesManager.services;
    const center = this._getSegmentCenter(segmentationId, segmentIndex);
    if (!center?.world) {
      return;
    }
    const {
      world
    } = center;

    // todo: generalize
    toolGroupId = toolGroupId || this._getToolGroupIdsWithSegmentation(segmentationId);
    const toolGroups = [];
    if (Array.isArray(toolGroupId)) {
      toolGroupId.forEach(toolGroup => {
        toolGroups.push(toolGroupService.getToolGroup(toolGroup));
      });
    } else {
      toolGroups.push(toolGroupService.getToolGroup(toolGroupId));
    }
    toolGroups.forEach(toolGroup => {
      const viewportsInfo = toolGroup.getViewportsInfo();

      // @ts-ignore
      for (const {
        viewportId,
        renderingEngineId
      } of viewportsInfo) {
        const {
          viewport
        } = (0,esm.getEnabledElementByIds)(viewportId, renderingEngineId);
        dist_esm.utilities.viewport.jumpToWorld(viewport, world);
      }
      if (highlightSegment) {
        this.highlightSegment(segmentationId, segmentIndex, toolGroup.id, highlightAlpha, animationLength, highlightHideOthers, highlightFunctionType);
      }
    });
  }
  highlightSegment(segmentationId, segmentIndex, toolGroupId) {
    let alpha = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : 0.9;
    let animationLength = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : 750;
    let hideOthers = arguments.length > 5 && arguments[5] !== undefined ? arguments[5] : true;
    let highlightFunctionType = arguments.length > 6 && arguments[6] !== undefined ? arguments[6] : 'ease-in-out';
    if (this.highlightIntervalId) {
      clearInterval(this.highlightIntervalId);
    }
    const segmentation = this.getSegmentation(segmentationId);
    toolGroupId = toolGroupId ?? this._getApplicableToolGroupId();
    const segmentationRepresentation = this._getSegmentationRepresentation(segmentationId, toolGroupId);
    const {
      type
    } = segmentationRepresentation;
    const {
      segments
    } = segmentation;
    const highlightFn = type === LABELMAP ? this._highlightLabelmap.bind(this) : this._highlightContour.bind(this);
    const adjustedAlpha = type === LABELMAP ? alpha : 1 - alpha;
    highlightFn(segmentIndex, adjustedAlpha, hideOthers, segments, toolGroupId, animationLength, segmentationRepresentation);
  }
  _setDisplaySetIsHydrated(displaySetUID, isHydrated) {
    const {
      displaySetService
    } = this.servicesManager.services;
    const displaySet = displaySetService.getDisplaySetByUID(displaySetUID);
    if (!displaySet) {
      return;
    }
    displaySet.isHydrated = isHydrated;
    displaySetService.setDisplaySetMetadataInvalidated(displaySetUID, false);
  }
  _highlightLabelmap(segmentIndex, alpha, hideOthers, segments, toolGroupId, animationLength, segmentationRepresentation) {
    const newSegmentSpecificConfig = {
      [segmentIndex]: {
        LABELMAP: {
          fillAlpha: alpha
        }
      }
    };
    if (hideOthers) {
      for (let i = 0; i < segments.length; i++) {
        if (i !== segmentIndex) {
          newSegmentSpecificConfig[i] = {
            LABELMAP: {
              fillAlpha: 0
            }
          };
        }
      }
    }
    const {
      fillAlpha
    } = this.getConfiguration(toolGroupId);
    let startTime = null;
    const animation = timestamp => {
      if (startTime === null) {
        startTime = timestamp;
      }
      const elapsed = timestamp - startTime;
      const progress = Math.min(elapsed / animationLength, 1);
      dist_esm.segmentation.config.setSegmentSpecificConfig(toolGroupId, segmentationRepresentation.segmentationRepresentationUID, {
        [segmentIndex]: {
          LABELMAP: {
            fillAlpha: easeInOutBell(progress, fillAlpha)
          }
        }
      });
      if (progress < 1) {
        requestAnimationFrame(animation);
      } else {
        dist_esm.segmentation.config.setSegmentSpecificConfig(toolGroupId, segmentationRepresentation.segmentationRepresentationUID, {});
      }
    };
    requestAnimationFrame(animation);
  }
  _highlightContour(segmentIndex, alpha, hideOthers, segments, toolGroupId, animationLength, segmentationRepresentation) {
    const startTime = performance.now();
    const animate = currentTime => {
      const progress = (currentTime - startTime) / animationLength;
      if (progress >= 1) {
        dist_esm.segmentation.config.setSegmentSpecificConfig(toolGroupId, segmentationRepresentation.segmentationRepresentationUID, {});
        return;
      }
      const reversedProgress = reverseEaseInOutBell(progress, 0.1);
      dist_esm.segmentation.config.setSegmentSpecificConfig(toolGroupId, segmentationRepresentation.segmentationRepresentationUID, {
        [segmentIndex]: {
          CONTOUR: {
            fillAlpha: reversedProgress
          }
        }
      });
      requestAnimationFrame(animate);
    };
    requestAnimationFrame(animate);
  }
  removeSegmentationRepresentationFromToolGroup(toolGroupId, segmentationRepresentationUIDsIds) {
    const uids = segmentationRepresentationUIDsIds || [];
    if (!uids.length) {
      const representations = dist_esm.segmentation.state.getSegmentationRepresentations(toolGroupId);
      if (!representations || !representations.length) {
        return;
      }
      uids.push(...representations.map(rep => rep.segmentationRepresentationUID));
    }
    dist_esm.segmentation.removeSegmentationsFromToolGroup(toolGroupId, uids);
  }

  /**
   * Removes a segmentation and broadcasts the removed event.
   *
   * @param {string} segmentationId The segmentation id
   */
  remove(segmentationId) {
    const segmentation = this.segmentations[segmentationId];
    const wasActive = segmentation.isActive;
    if (!segmentationId || !segmentation) {
      console.warn(`No segmentationId provided, or unable to find segmentation by id.`);
      return;
    }
    const {
      colorLUTIndex
    } = segmentation;
    this._removeSegmentationFromCornerstone(segmentationId);

    // Delete associated colormap
    // Todo: bring this back
    dist_esm.segmentation.state.removeColorLUT(colorLUTIndex);
    delete this.segmentations[segmentationId];

    // If this segmentation was active, and there is another segmentation, set another one active.

    if (wasActive) {
      const remainingSegmentations = this._getSegmentations();
      const remainingHydratedSegmentations = remainingSegmentations.filter(segmentation => segmentation.hydrated);
      if (remainingHydratedSegmentations.length) {
        const {
          id
        } = remainingHydratedSegmentations[0];
        this._setActiveSegmentationForToolGroup(id, this._getApplicableToolGroupId(), false);
      }
    }
    this._setDisplaySetIsHydrated(segmentationId, false);
    this._broadcastEvent(this.EVENTS.SEGMENTATION_REMOVED, {
      segmentationId
    });
  }
  setSegmentLabel(segmentationId, segmentIndex, label) {
    this._setSegmentLabel(segmentationId, segmentIndex, label);
  }
  _setSegmentLabel(segmentationId, segmentIndex, label) {
    let suppressEvents = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : false;
    const segmentation = this.getSegmentation(segmentationId);
    if (segmentation === undefined) {
      throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
    }
    const segmentInfo = segmentation.segments[segmentIndex];
    if (segmentInfo === undefined) {
      throw new Error(`Segment ${segmentIndex} not yet added to segmentation: ${segmentationId}`);
    }
    segmentInfo.label = label;
    if (suppressEvents === false) {
      // this._setSegmentationModified(segmentationId);
      this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
        segmentation
      });
    }
  }
  shouldRenderSegmentation(viewportDisplaySetInstanceUIDs, segmentationFrameOfReferenceUID) {
    if (!viewportDisplaySetInstanceUIDs?.length) {
      return false;
    }
    const {
      displaySetService
    } = this.servicesManager.services;
    let shouldDisplaySeg = false;

    // check if the displaySet is sharing the same frameOfReferenceUID
    // with the new segmentation
    for (const displaySetInstanceUID of viewportDisplaySetInstanceUIDs) {
      const displaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);

      // Todo: this might not be ideal for use cases such as 4D, since we
      // don't want to show the segmentation for all the frames
      if (displaySet.isReconstructable && displaySet?.images?.[0]?.FrameOfReferenceUID === segmentationFrameOfReferenceUID) {
        shouldDisplaySeg = true;
        break;
      }
    }
    return shouldDisplaySeg;
  }
  _getDefaultSegmentationScheme() {
    return {
      activeSegmentIndex: 1,
      cachedStats: {},
      label: '',
      segmentsLocked: [],
      displayText: [],
      hydrated: false,
      // by default we don't hydrate the segmentation for SEG displaySets
      segmentCount: 0,
      segments: [],
      isVisible: true,
      isActive: false,
      colorLUTIndex: 0
    };
  }
  _setActiveSegmentationForToolGroup(segmentationId, toolGroupId) {
    let suppressEvents = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : false;
    const segmentations = this._getSegmentations();
    const targetSegmentation = this.getSegmentation(segmentationId);
    if (targetSegmentation === undefined) {
      throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
    }
    segmentations.forEach(segmentation => {
      segmentation.isActive = segmentation.id === segmentationId;
    });
    const representation = this._getSegmentationRepresentation(segmentationId, toolGroupId);
    dist_esm.segmentation.activeSegmentation.setActiveSegmentationRepresentation(toolGroupId, representation.segmentationRepresentationUID);
    if (suppressEvents === false) {
      this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
        segmentation: targetSegmentation
      });
    }
  }
  _setActiveSegment(segmentationId, segmentIndex) {
    let suppressEvents = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : false;
    const segmentation = this.getSegmentation(segmentationId);
    if (segmentation === undefined) {
      throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
    }
    dist_esm.segmentation.segmentIndex.setActiveSegmentIndex(segmentationId, segmentIndex);
    segmentation.activeSegmentIndex = segmentIndex;
    if (suppressEvents === false) {
      this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
        segmentation
      });
    }
  }
  _getSegmentInfo(segmentation, segmentIndex) {
    const segments = segmentation.segments;
    if (!segments) {
      return;
    }
    if (segments && segments.length > 0) {
      return segments[segmentIndex];
    }
  }
  _getVolumeIdForDisplaySet(displaySet) {
    const volumeLoaderSchema = displaySet.volumeLoaderSchema ?? VOLUME_LOADER_SCHEME;
    return `${volumeLoaderSchema}:${displaySet.displaySetInstanceUID}`;
  }
  _getSegmentCenter(segmentationId, segmentIndex) {
    const segmentation = this.getSegmentation(segmentationId);
    if (!segmentation) {
      return;
    }
    const {
      cachedStats
    } = segmentation;
    if (!cachedStats) {
      return;
    }
    const {
      segmentCenter
    } = cachedStats;
    if (!segmentCenter) {
      return;
    }
    const {
      center
    } = segmentCenter[segmentIndex];
    return center;
  }
  _setSegmentLocked(segmentationId, segmentIndex, isLocked) {
    let suppressEvents = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : false;
    const segmentation = this.getSegmentation(segmentationId);
    if (segmentation === undefined) {
      throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
    }
    const segmentInfo = this._getSegmentInfo(segmentation, segmentIndex);
    if (segmentInfo === undefined) {
      throw new Error(`Segment ${segmentIndex} not yet added to segmentation: ${segmentationId}`);
    }
    segmentInfo.isLocked = isLocked;
    dist_esm.segmentation.segmentLocking.setSegmentIndexLocked(segmentationId, segmentIndex, isLocked);
    if (suppressEvents === false) {
      this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
        segmentation
      });
    }
  }
  _setSegmentVisibility(segmentationId, segmentIndex, isVisible, toolGroupId) {
    let suppressEvents = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : false;
    toolGroupId = toolGroupId ?? this._getApplicableToolGroupId();
    const {
      segmentationRepresentationUID,
      segmentation
    } = this._getSegmentationInfo(segmentationId, toolGroupId);
    if (segmentation === undefined) {
      throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
    }
    const segmentInfo = this._getSegmentInfo(segmentation, segmentIndex);
    if (segmentInfo === undefined) {
      throw new Error(`Segment ${segmentIndex} not yet added to segmentation: ${segmentationId}`);
    }
    segmentInfo.isVisible = isVisible;
    dist_esm.segmentation.config.visibility.setSegmentVisibility(toolGroupId, segmentationRepresentationUID, segmentIndex, isVisible);

    // make sure to update the isVisible flag on the segmentation
    // if a segment becomes invisible then the segmentation should be invisible
    // in the status as well, and show correct icon
    segmentation.isVisible = segmentation.segments.filter(Boolean).every(segment => segment.isVisible);
    if (suppressEvents === false) {
      this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
        segmentation
      });
    }
  }
  _setSegmentLabel(segmentationId, segmentIndex, segmentLabel) {
    let suppressEvents = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : false;
    const segmentation = this.getSegmentation(segmentationId);
    if (segmentation === undefined) {
      throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
    }
    const segmentInfo = this._getSegmentInfo(segmentation, segmentIndex);
    if (segmentInfo === undefined) {
      throw new Error(`Segment ${segmentIndex} not yet added to segmentation: ${segmentationId}`);
    }
    segmentInfo.label = segmentLabel;
    if (suppressEvents === false) {
      this._broadcastEvent(this.EVENTS.SEGMENTATION_UPDATED, {
        segmentation
      });
    }
  }
  _getSegmentationRepresentation(segmentationId, toolGroupId) {
    const segmentationRepresentations = this.getSegmentationRepresentationsForToolGroup(toolGroupId);
    if (!segmentationRepresentations?.length) {
      return;
    }

    // Todo: this finds the first segmentation representation that matches the segmentationId
    // If there are two labelmap representations from the same segmentation, this will not work
    const representation = segmentationRepresentations.find(representation => representation.segmentationId === segmentationId);
    return representation;
  }
  _initSegmentationService() {
    // Connect Segmentation Service to Cornerstone3D.
    esm.eventTarget.addEventListener(dist_esm.Enums.Events.SEGMENTATION_MODIFIED, this._onSegmentationModifiedFromSource);
    esm.eventTarget.addEventListener(dist_esm.Enums.Events.SEGMENTATION_DATA_MODIFIED, this._onSegmentationDataModified);
  }
  _getSegmentationInfo(segmentationId, toolGroupId) {
    const segmentation = this.getSegmentation(segmentationId);
    if (segmentation === undefined) {
      throw new Error(`no segmentation for segmentationId: ${segmentationId}`);
    }
    const segmentationRepresentation = this._getSegmentationRepresentation(segmentationId, toolGroupId);
    if (!segmentationRepresentation) {
      throw new Error('Must add representation to toolgroup before setting segments');
    }
    const {
      segmentationRepresentationUID
    } = segmentationRepresentation;
    return {
      segmentationRepresentationUID,
      segmentation
    };
  }
  _removeSegmentationFromCornerstone(segmentationId) {
    // TODO: This should be from the configuration
    const removeFromCache = true;
    const segmentationState = dist_esm.segmentation.state;
    const sourceSegState = segmentationState.getSegmentation(segmentationId);
    if (!sourceSegState) {
      return;
    }
    const toolGroupIds = segmentationState.getToolGroupIdsWithSegmentation(segmentationId);
    toolGroupIds.forEach(toolGroupId => {
      const segmentationRepresentations = segmentationState.getSegmentationRepresentations(toolGroupId);
      const UIDsToRemove = [];
      segmentationRepresentations.forEach(representation => {
        if (representation.segmentationId === segmentationId) {
          UIDsToRemove.push(representation.segmentationRepresentationUID);
        }
      });

      // remove segmentation representations
      dist_esm.segmentation.removeSegmentationsFromToolGroup(toolGroupId, UIDsToRemove, true // immediate
      );
    });

    // cleanup the segmentation state too
    segmentationState.removeSegmentation(segmentationId);
    if (removeFromCache && esm.cache.getVolumeLoadObject(segmentationId)) {
      esm.cache.removeVolumeLoadObject(segmentationId);
    }
  }
  _updateCornerstoneSegmentations(_ref3) {
    let {
      segmentationId,
      notYetUpdatedAtSource
    } = _ref3;
    if (notYetUpdatedAtSource === false) {
      return;
    }
    const segmentationState = dist_esm.segmentation.state;
    const sourceSegmentation = segmentationState.getSegmentation(segmentationId);
    const segmentation = this.segmentations[segmentationId];
    const {
      label,
      cachedStats
    } = segmentation;

    // Update the label in the source if necessary
    if (sourceSegmentation.label !== label) {
      sourceSegmentation.label = label;
    }
    if (!lodash_isequal_default()(sourceSegmentation.cachedStats, cachedStats)) {
      sourceSegmentation.cachedStats = cachedStats;
    }
  }
  _getToolGroupIdsWithSegmentation(segmentationId) {
    const segmentationState = dist_esm.segmentation.state;
    const toolGroupIds = segmentationState.getToolGroupIdsWithSegmentation(segmentationId);
    return toolGroupIds;
  }
  _getFrameOfReferenceUIDForSeg(displaySet) {
    const frameOfReferenceUID = displaySet.instance?.FrameOfReferenceUID;
    if (frameOfReferenceUID) {
      return frameOfReferenceUID;
    }

    // if not found we should try the ReferencedFrameOfReferenceSequence
    const referencedFrameOfReferenceSequence = displaySet.instance?.ReferencedFrameOfReferenceSequence;
    if (referencedFrameOfReferenceSequence) {
      return referencedFrameOfReferenceSequence.FrameOfReferenceUID;
    }
  }
  generateNewColorLUT() {
    const newColorLUT = lodash_clonedeep_default()(COLOR_LUT);
    return newColorLUT;
  }
}
SegmentationService.REGISTRATION = {
  name: 'segmentationService',
  altName: 'SegmentationService',
  create: _ref4 => {
    let {
      servicesManager
    } = _ref4;
    return new SegmentationService({
      servicesManager
    });
  }
};
/* harmony default export */ const SegmentationService_SegmentationService = (SegmentationService);

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/SegmentationService/index.js

/* harmony default export */ const services_SegmentationService = (SegmentationService_SegmentationService);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/getCornerstoneViewportType.ts

const STACK = 'stack';
const VOLUME = 'volume';
const ORTHOGRAPHIC = 'orthographic';
const VOLUME_3D = 'volume3d';
function getCornerstoneViewportType(viewportType) {
  const lowerViewportType = viewportType.toLowerCase();
  if (lowerViewportType === STACK) {
    return esm.Enums.ViewportType.STACK;
  }
  if (lowerViewportType === VOLUME || lowerViewportType === ORTHOGRAPHIC) {
    return esm.Enums.ViewportType.ORTHOGRAPHIC;
  }
  if (lowerViewportType === VOLUME_3D) {
    return esm.Enums.ViewportType.VOLUME_3D;
  }
  throw new Error(`Invalid viewport type: ${viewportType}. Valid types are: stack, volume`);
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/CornerstoneCacheService/CornerstoneCacheService.ts


const CornerstoneCacheService_VOLUME_LOADER_SCHEME = 'cornerstoneStreamingImageVolume';
class CornerstoneCacheService {
  constructor(servicesManager) {
    this.stackImageIds = new Map();
    this.volumeImageIds = new Map();
    this.servicesManager = void 0;
    this.servicesManager = servicesManager;
  }
  getCacheSize() {
    return esm.cache.getCacheSize();
  }
  getCacheFreeSpace() {
    return esm.cache.getBytesAvailable();
  }
  async createViewportData(displaySets, viewportOptions, dataSource, initialImageIndex) {
    let viewportType = viewportOptions.viewportType;

    // Todo: Since Cornerstone 3D currently doesn't support segmentation
    // on stack viewport, we should check if whether the the displaySets
    // that are about to be displayed are referenced in a segmentation
    // as a reference volume, if so, we should hang a volume viewport
    // instead of a stack viewport
    if (this._shouldRenderSegmentation(displaySets)) {
      viewportType = 'volume';

      // update viewportOptions to reflect the new viewport type
      viewportOptions.viewportType = viewportType;
    }
    const cs3DViewportType = getCornerstoneViewportType(viewportType);
    let viewportData;
    if (cs3DViewportType === esm.Enums.ViewportType.STACK) {
      viewportData = await this._getStackViewportData(dataSource, displaySets, initialImageIndex, cs3DViewportType);
    }
    if (cs3DViewportType === esm.Enums.ViewportType.ORTHOGRAPHIC || cs3DViewportType === esm.Enums.ViewportType.VOLUME_3D) {
      viewportData = await this._getVolumeViewportData(dataSource, displaySets, cs3DViewportType);
    }
    viewportData.viewportType = cs3DViewportType;
    return viewportData;
  }
  async invalidateViewportData(viewportData, invalidatedDisplaySetInstanceUID, dataSource, displaySetService) {
    if (viewportData.viewportType === esm.Enums.ViewportType.STACK) {
      return this._getCornerstoneStackImageIds(displaySetService.getDisplaySetByUID(invalidatedDisplaySetInstanceUID), dataSource);
    }

    // Todo: grab the volume and get the id from the viewport itself
    const volumeId = `${CornerstoneCacheService_VOLUME_LOADER_SCHEME}:${invalidatedDisplaySetInstanceUID}`;
    const volume = esm.cache.getVolume(volumeId);
    if (volume) {
      esm.cache.removeVolumeLoadObject(volumeId);
      this.volumeImageIds.delete(volumeId);
    }
    const displaySets = viewportData.data.map(_ref => {
      let {
        displaySetInstanceUID
      } = _ref;
      return displaySetService.getDisplaySetByUID(displaySetInstanceUID);
    });
    const newViewportData = await this._getVolumeViewportData(dataSource, displaySets, viewportData.viewportType);
    return newViewportData;
  }
  _getStackViewportData(dataSource, displaySets, initialImageIndex, viewportType) {
    // For Stack Viewport we don't have fusion currently
    const displaySet = displaySets[0];
    let stackImageIds = this.stackImageIds.get(displaySet.displaySetInstanceUID);
    if (!stackImageIds) {
      stackImageIds = this._getCornerstoneStackImageIds(displaySet, dataSource);
      this.stackImageIds.set(displaySet.displaySetInstanceUID, stackImageIds);
    }
    const {
      displaySetInstanceUID,
      StudyInstanceUID,
      isCompositeStack
    } = displaySet;
    const StackViewportData = {
      viewportType,
      data: {
        StudyInstanceUID,
        displaySetInstanceUID,
        isCompositeStack,
        imageIds: stackImageIds
      }
    };
    if (typeof initialImageIndex === 'number') {
      StackViewportData.data.initialImageIndex = initialImageIndex;
    }
    return StackViewportData;
  }
  async _getVolumeViewportData(dataSource, displaySets, viewportType) {
    // Todo: Check the cache for multiple scenarios to see if we need to
    // decache the volume data from other viewports or not

    const volumeData = [];
    for (const displaySet of displaySets) {
      // Don't create volumes for the displaySets that have custom load
      // function (e.g., SEG, RT, since they rely on the reference volumes
      // and they take care of their own loading after they are created in their
      // getSOPClassHandler method

      if (displaySet.load && displaySet.load instanceof Function) {
        const {
          userAuthenticationService
        } = this.servicesManager.services;
        const headers = userAuthenticationService.getAuthorizationHeader();
        await displaySet.load({
          headers
        });
        volumeData.push({
          studyInstanceUID: displaySet.StudyInstanceUID,
          displaySetInstanceUID: displaySet.displaySetInstanceUID
        });

        // Todo: do some cache check and empty the cache if needed
        continue;
      }
      const volumeLoaderSchema = displaySet.volumeLoaderSchema ?? CornerstoneCacheService_VOLUME_LOADER_SCHEME;
      const volumeId = `${volumeLoaderSchema}:${displaySet.displaySetInstanceUID}`;
      let volumeImageIds = this.volumeImageIds.get(displaySet.displaySetInstanceUID);
      let volume = esm.cache.getVolume(volumeId);
      if (!volumeImageIds || !volume) {
        volumeImageIds = this._getCornerstoneVolumeImageIds(displaySet, dataSource);
        volume = await esm.volumeLoader.createAndCacheVolume(volumeId, {
          imageIds: volumeImageIds
        });
        this.volumeImageIds.set(displaySet.displaySetInstanceUID, volumeImageIds);
      }
      volumeData.push({
        StudyInstanceUID: displaySet.StudyInstanceUID,
        displaySetInstanceUID: displaySet.displaySetInstanceUID,
        volume,
        volumeId,
        imageIds: volumeImageIds
      });
    }
    return {
      viewportType,
      data: volumeData
    };
  }
  _shouldRenderSegmentation(displaySets) {
    const {
      segmentationService,
      displaySetService
    } = this.servicesManager.services;
    const viewportDisplaySetInstanceUIDs = displaySets.map(_ref2 => {
      let {
        displaySetInstanceUID
      } = _ref2;
      return displaySetInstanceUID;
    });

    // check inside segmentations if any of them are referencing the displaySets
    // that are about to be displayed
    const segmentations = segmentationService.getSegmentations();
    for (const segmentation of segmentations) {
      const segDisplaySetInstanceUID = segmentation.displaySetInstanceUID;
      const segDisplaySet = displaySetService.getDisplaySetByUID(segDisplaySetInstanceUID);
      const instance = segDisplaySet.instances?.[0] || segDisplaySet.instance;
      const shouldDisplaySeg = segmentationService.shouldRenderSegmentation(viewportDisplaySetInstanceUIDs, instance.FrameOfReferenceUID);
      if (shouldDisplaySeg) {
        return true;
      }
    }
  }
  _getCornerstoneStackImageIds(displaySet, dataSource) {
    return dataSource.getImageIdsForDisplaySet(displaySet);
  }
  _getCornerstoneVolumeImageIds(displaySet, dataSource) {
    const stackImageIds = this._getCornerstoneStackImageIds(displaySet, dataSource);
    return stackImageIds;
  }
}
CornerstoneCacheService.REGISTRATION = {
  name: 'cornerstoneCacheService',
  altName: 'CornerstoneCacheService',
  create: _ref3 => {
    let {
      servicesManager
    } = _ref3;
    return new CornerstoneCacheService(servicesManager);
  }
};
/* harmony default export */ const CornerstoneCacheService_CornerstoneCacheService = (CornerstoneCacheService);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/CornerstoneCacheService/index.js

/* harmony default export */ const services_CornerstoneCacheService = (CornerstoneCacheService_CornerstoneCacheService);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/ViewportService/constants.ts
const RENDERING_ENGINE_ID = 'OHIFCornerstoneRenderingEngine';

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/getCornerstoneBlendMode.ts

const MIP = 'mip';
function getCornerstoneBlendMode(blendMode) {
  if (!blendMode) {
    return esm.Enums.BlendModes.COMPOSITE;
  }
  if (blendMode.toLowerCase() === MIP) {
    return esm.Enums.BlendModes.MAXIMUM_INTENSITY_BLEND;
  }
  throw new Error();
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/getCornerstoneOrientation.ts

const AXIAL = 'axial';
const SAGITTAL = 'sagittal';
const CORONAL = 'coronal';
function getCornerstoneOrientation(orientation) {
  if (orientation) {
    switch (orientation.toLowerCase()) {
      case AXIAL:
        return esm.Enums.OrientationAxis.AXIAL;
      case SAGITTAL:
        return esm.Enums.OrientationAxis.SAGITTAL;
      case CORONAL:
        return esm.Enums.OrientationAxis.CORONAL;
      default:
        return esm.Enums.OrientationAxis.ACQUISITION;
    }
  }
  return esm.Enums.OrientationAxis.ACQUISITION;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/ViewportService/Viewport.ts




const Viewport_STACK = 'stack';
const DEFAULT_TOOLGROUP_ID = 'default';

// Return true if the data contains the given display set UID OR the imageId
// if it is a composite object.
const dataContains = (data, displaySetUID, imageId) => {
  if (data.displaySetInstanceUID === displaySetUID) {
    return true;
  }
  if (imageId && data.isCompositeStack && data.imageIds) {
    return !!data.imageIds.find(dataId => dataId === imageId);
  }
  return false;
};
class ViewportInfo {
  constructor(viewportId) {
    this.viewportId = '';
    this.element = void 0;
    this.viewportOptions = void 0;
    this.displaySetOptions = void 0;
    this.viewportData = void 0;
    this.renderingEngineId = void 0;
    this.destroy = () => {
      this.element = null;
      this.viewportData = null;
      this.viewportOptions = null;
      this.displaySetOptions = null;
    };
    this.viewportId = viewportId;
    this.setPublicViewportOptions({});
    this.setPublicDisplaySetOptions([{}]);
  }

  /**
   * Return true if the viewport contains the given display set UID,
   * OR if it is a composite stack and contains the given imageId
   */
  contains(displaySetUID, imageId) {
    if (!this.viewportData?.data) {
      return false;
    }
    if (this.viewportData.data.length) {
      return !!this.viewportData.data.find(data => dataContains(data, displaySetUID, imageId));
    }
    return dataContains(this.viewportData.data, displaySetUID, imageId);
  }
  setRenderingEngineId(renderingEngineId) {
    this.renderingEngineId = renderingEngineId;
  }
  getRenderingEngineId() {
    return this.renderingEngineId;
  }
  setViewportId(viewportId) {
    this.viewportId = viewportId;
  }
  setElement(element) {
    this.element = element;
  }
  setViewportData(viewportData) {
    this.viewportData = viewportData;
  }
  getViewportData() {
    return this.viewportData;
  }
  getElement() {
    return this.element;
  }
  getViewportId() {
    return this.viewportId;
  }
  setPublicDisplaySetOptions(publicDisplaySetOptions) {
    // map the displaySetOptions and check if they are undefined then set them to default values
    const displaySetOptions = this.mapDisplaySetOptions(publicDisplaySetOptions);
    this.setDisplaySetOptions(displaySetOptions);
    return this.displaySetOptions;
  }
  hasDisplaySet(displaySetInstanceUID) {
    // Todo: currently this does not work for non image & referenceImage displaySets.
    // Since SEG and other derived displaySets are loaded in a different way, and not
    // via cornerstoneViewportService
    let viewportData = this.getViewportData();
    if (viewportData.viewportType === esm.Enums.ViewportType.ORTHOGRAPHIC || viewportData.viewportType === esm.Enums.ViewportType.VOLUME_3D) {
      viewportData = viewportData;
      return viewportData.data.some(_ref => {
        let {
          displaySetInstanceUID: dsUID
        } = _ref;
        return dsUID === displaySetInstanceUID;
      });
    }
    viewportData = viewportData;
    return viewportData.data.displaySetInstanceUID === displaySetInstanceUID;
  }
  setPublicViewportOptions(viewportOptionsEntry) {
    let viewportType = viewportOptionsEntry.viewportType;
    const {
      toolGroupId = DEFAULT_TOOLGROUP_ID,
      presentationIds
    } = viewportOptionsEntry;
    let orientation;
    if (!viewportType) {
      viewportType = getCornerstoneViewportType(Viewport_STACK);
    } else {
      viewportType = getCornerstoneViewportType(viewportOptionsEntry.viewportType);
    }

    // map SAGITTAL, AXIAL, CORONAL orientation to be used by cornerstone
    if (viewportOptionsEntry.viewportType?.toLowerCase() !== Viewport_STACK) {
      orientation = getCornerstoneOrientation(viewportOptionsEntry.orientation);
    }
    if (!toolGroupId) {
      toolGroupId = DEFAULT_TOOLGROUP_ID;
    }
    this.setViewportOptions({
      ...viewportOptionsEntry,
      viewportId: this.viewportId,
      viewportType: viewportType,
      orientation,
      toolGroupId,
      presentationIds
    });
    return this.viewportOptions;
  }
  setViewportOptions(viewportOptions) {
    this.viewportOptions = viewportOptions;
  }
  getViewportOptions() {
    return this.viewportOptions;
  }
  setDisplaySetOptions(displaySetOptions) {
    this.displaySetOptions = displaySetOptions;
  }
  getSyncGroups() {
    this.viewportOptions.syncGroups ||= [];
    return this.viewportOptions.syncGroups;
  }
  getDisplaySetOptions() {
    return this.displaySetOptions;
  }
  getViewportType() {
    return this.viewportOptions.viewportType || esm.Enums.ViewportType.STACK;
  }
  getToolGroupId() {
    return this.viewportOptions.toolGroupId;
  }
  getBackground() {
    return this.viewportOptions.background || [0, 0, 0];
  }
  getOrientation() {
    return this.viewportOptions.orientation;
  }
  getDisplayArea() {
    return this.viewportOptions.displayArea;
  }
  getInitialImageOptions() {
    return this.viewportOptions.initialImageOptions;
  }

  // Handle incoming public display set options or a display set select
  // with a contained options.
  mapDisplaySetOptions() {
    let options = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : [{}];
    const displaySetOptions = [];
    options.forEach(item => {
      let option = item?.options || item;
      if (!option) {
        option = {
          blendMode: undefined,
          slabThickness: undefined,
          colormap: undefined,
          voi: {},
          voiInverted: false
        };
      }
      const blendMode = getCornerstoneBlendMode(option.blendMode);
      displaySetOptions.push({
        voi: option.voi,
        voiInverted: option.voiInverted,
        colormap: option.colormap,
        slabThickness: option.slabThickness,
        blendMode,
        displayPreset: option.displayPreset
      });
    });
    return displaySetOptions;
  }
}
/* harmony default export */ const Viewport = (ViewportInfo);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/JumpPresets.ts
/**
 * Jump Presets - This enum defines the 3 jump states which are available
 * to be used with the jumpToSlice utility function.
 */
var JumpPresets = /*#__PURE__*/function (JumpPresets) {
  JumpPresets["First"] = "first";
  JumpPresets["Last"] = "last";
  JumpPresets["Middle"] = "middle";
  return JumpPresets;
}(JumpPresets || {});
/* harmony default export */ const utils_JumpPresets = (JumpPresets);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/services/ViewportService/CornerstoneViewportService.ts






const CornerstoneViewportService_EVENTS = {
  VIEWPORT_DATA_CHANGED: 'event::cornerstoneViewportService:viewportDataChanged'
};

/**
 * Handles cornerstone viewport logic including enabling, disabling, and
 * updating the viewport.
 */
class CornerstoneViewportService extends src/* PubSubService */.hC {
  constructor(servicesManager) {
    super(CornerstoneViewportService_EVENTS);
    this.renderingEngine = void 0;
    this.viewportsById = new Map();
    this.viewportGridResizeObserver = void 0;
    this.viewportsDisplaySets = new Map();
    // Some configs
    this.enableResizeDetector = void 0;
    this.resizeRefreshRateMs = void 0;
    this.resizeRefreshMode = void 0;
    this.servicesManager = null;
    this.renderingEngine = null;
    this.viewportGridResizeObserver = null;
    this.servicesManager = servicesManager;
  }

  /**
   * Adds the HTML element to the viewportService
   * @param {*} viewportId
   * @param {*} elementRef
   */
  enableViewport(viewportId, elementRef) {
    const viewportInfo = new Viewport(viewportId);
    viewportInfo.setElement(elementRef);
    this.viewportsById.set(viewportId, viewportInfo);
  }
  getViewportIds() {
    return Array.from(this.viewportsById.keys());
  }

  /**
   * It retrieves the renderingEngine if it does exist, or creates one otherwise
   * @returns {RenderingEngine} rendering engine
   */
  getRenderingEngine() {
    // get renderingEngine from cache if it exists
    const renderingEngine = (0,esm.getRenderingEngine)(RENDERING_ENGINE_ID);
    if (renderingEngine) {
      this.renderingEngine = renderingEngine;
      return this.renderingEngine;
    }
    if (!renderingEngine || renderingEngine.hasBeenDestroyed) {
      this.renderingEngine = new esm.RenderingEngine(RENDERING_ENGINE_ID);
    }
    return this.renderingEngine;
  }

  /**
   * It triggers the resize on the rendering engine.
   */
  resize() {
    const immediate = true;
    const keepCamera = true;
    this.renderingEngine.resize(immediate, keepCamera);
    this.renderingEngine.render();
  }

  /**
   * Removes the viewport from cornerstone, and destroys the rendering engine
   */
  destroy() {
    this._removeResizeObserver();
    this.viewportGridResizeObserver = null;
    try {
      this.renderingEngine?.destroy?.();
    } catch (e) {
      console.warn('Rendering engine not destroyed', e);
    }
    this.viewportsDisplaySets.clear();
    this.renderingEngine = null;
    esm.cache.purgeCache();
  }

  /**
   * Disables the viewport inside the renderingEngine, if no viewport is left
   * it destroys the renderingEngine.
   *
   * This is called when the element goes away entirely - with new viewportId's
   * created for every new viewport, this will be called whenever the set of
   * viewports is changed, but NOT when the viewport position changes only.
   *
   * @param viewportId - The viewportId to disable
   */
  disableElement(viewportId) {
    this.renderingEngine?.disableElement(viewportId);

    // clean up
    this.viewportsById.delete(viewportId);
    this.viewportsDisplaySets.delete(viewportId);
  }
  setPresentations(viewport, presentations) {
    const properties = presentations?.lutPresentation?.properties;
    if (properties) {
      viewport.setProperties(properties);
    }
    const camera = presentations?.positionPresentation?.camera;
    if (camera) {
      viewport.setCamera(camera);
    }
  }
  getPresentation(viewportId) {
    const viewportInfo = this.viewportsById.get(viewportId);
    if (!viewportInfo) {
      return;
    }
    const {
      viewportType,
      presentationIds
    } = viewportInfo.getViewportOptions();
    const csViewport = this.getCornerstoneViewport(viewportId);
    if (!csViewport) {
      return;
    }
    const properties = csViewport.getProperties();
    if (properties.isComputedVOI) {
      delete properties.voiRange;
      delete properties.VOILUTFunction;
    }
    const initialImageIndex = csViewport.getCurrentImageIdIndex();
    const camera = csViewport.getCamera();
    return {
      presentationIds,
      viewportType: !viewportType || viewportType === 'stack' ? 'stack' : 'volume',
      properties,
      initialImageIndex,
      camera
    };
  }
  storePresentation(_ref) {
    let {
      viewportId
    } = _ref;
    const stateSyncService = this.servicesManager.services.stateSyncService;
    let presentation;
    try {
      presentation = this.getPresentation(viewportId);
    } catch (error) {
      console.warn(error);
    }
    if (!presentation || !presentation.presentationIds) {
      return;
    }
    const {
      lutPresentationStore,
      positionPresentationStore
    } = stateSyncService.getState();
    const {
      presentationIds
    } = presentation;
    const {
      lutPresentationId,
      positionPresentationId
    } = presentationIds || {};
    const storeState = {};
    if (lutPresentationId) {
      storeState.lutPresentationStore = {
        ...lutPresentationStore,
        [lutPresentationId]: presentation
      };
    }
    if (positionPresentationId) {
      storeState.positionPresentationStore = {
        ...positionPresentationStore,
        [positionPresentationId]: presentation
      };
    }
    stateSyncService.store(storeState);
  }

  /**
   * Sets the viewport data for a viewport.
   * @param viewportId - The ID of the viewport to set the data for.
   * @param viewportData - The viewport data to set.
   * @param publicViewportOptions - The public viewport options.
   * @param publicDisplaySetOptions - The public display set options.
   * @param presentations - The presentations to set.
   */
  setViewportData(viewportId, viewportData, publicViewportOptions, publicDisplaySetOptions, presentations) {
    const renderingEngine = this.getRenderingEngine();

    // This is the old viewportInfo, which may have old options but we might be
    // using its viewport (same viewportId as the new viewportInfo)
    const viewportInfo = this.viewportsById.get(viewportId);

    // We should store the presentation for the current viewport since we can't only
    // rely to store it WHEN the viewport is disabled since we might keep around the
    // same viewport/element and just change the viewportData for it (drag and drop etc.)
    // the disableElement storePresentation handle would not be called in this case
    // and we would lose the presentation.
    this.storePresentation({
      viewportId: viewportInfo.getViewportId()
    });
    if (!viewportInfo) {
      throw new Error('element is not enabled for the given viewportId');
    }

    // override the viewportOptions and displaySetOptions with the public ones
    // since those are the newly set ones, we set them here so that it handles defaults
    const displaySetOptions = viewportInfo.setPublicDisplaySetOptions(publicDisplaySetOptions);
    const viewportOptions = viewportInfo.setPublicViewportOptions(publicViewportOptions);
    const element = viewportInfo.getElement();
    const type = viewportInfo.getViewportType();
    const background = viewportInfo.getBackground();
    const orientation = viewportInfo.getOrientation();
    const displayArea = viewportInfo.getDisplayArea();
    const viewportInput = {
      viewportId,
      element,
      type,
      defaultOptions: {
        background,
        orientation,
        displayArea
      }
    };

    // Rendering Engine Id set should happen before enabling the element
    // since there are callbacks that depend on the renderingEngine id
    // Todo: however, this is a limitation which means that we can't change
    // the rendering engine id for a given viewport which might be a super edge
    // case
    viewportInfo.setRenderingEngineId(renderingEngine.id);

    // Todo: this is not optimal at all, we are re-enabling the already enabled
    // element which is not what we want. But enabledElement as part of the
    // renderingEngine is designed to be used like this. This will trigger
    // ENABLED_ELEMENT again and again, which will run onEnableElement callbacks
    renderingEngine.enableElement(viewportInput);
    viewportInfo.setViewportOptions(viewportOptions);
    viewportInfo.setDisplaySetOptions(displaySetOptions);
    viewportInfo.setViewportData(viewportData);
    viewportInfo.setViewportId(viewportId);
    this.viewportsById.set(viewportId, viewportInfo);
    const viewport = renderingEngine.getViewport(viewportId);
    this._setDisplaySets(viewport, viewportData, viewportInfo, presentations);

    // The broadcast event here ensures that listeners have a valid, up to date
    // viewport to access.  Doing it too early can result in exceptions or
    // invalid data.
    this._broadcastEvent(this.EVENTS.VIEWPORT_DATA_CHANGED, {
      viewportData,
      viewportId
    });
  }
  getCornerstoneViewport(viewportId) {
    const viewportInfo = this.getViewportInfo(viewportId);
    if (!viewportInfo || !this.renderingEngine || this.renderingEngine.hasBeenDestroyed) {
      return null;
    }
    const viewport = this.renderingEngine.getViewport(viewportId);
    return viewport;
  }
  getViewportInfo(viewportId) {
    return this.viewportsById.get(viewportId);
  }
  _setStackViewport(viewport, viewportData, viewportInfo, presentations) {
    const displaySetOptions = viewportInfo.getDisplaySetOptions();
    const {
      imageIds,
      initialImageIndex,
      displaySetInstanceUID
    } = viewportData.data;
    this.viewportsDisplaySets.set(viewport.id, [displaySetInstanceUID]);
    let initialImageIndexToUse = presentations?.positionPresentation?.initialImageIndex ?? initialImageIndex;
    if (initialImageIndexToUse === undefined || initialImageIndexToUse === null) {
      initialImageIndexToUse = this._getInitialImageIndexForViewport(viewportInfo, imageIds) || 0;
    }
    const properties = {
      ...presentations.lutPresentation?.properties
    };
    if (!presentations.lutPresentation?.properties) {
      const {
        voi,
        voiInverted
      } = displaySetOptions[0];
      if (voi && (voi.windowWidth || voi.windowCenter)) {
        const {
          lower,
          upper
        } = esm.utilities.windowLevel.toLowHighRange(voi.windowWidth, voi.windowCenter);
        properties.voiRange = {
          lower,
          upper
        };
      }
      if (voiInverted !== undefined) {
        properties.invert = voiInverted;
      }
    }
    viewport.setStack(imageIds, initialImageIndexToUse).then(() => {
      viewport.setProperties({
        ...properties
      });
      const camera = presentations.positionPresentation?.camera;
      if (camera) {
        viewport.setCamera(camera);
      }
    });
  }
  _getInitialImageIndexForViewport(viewportInfo, imageIds) {
    const initialImageOptions = viewportInfo.getInitialImageOptions();
    if (!initialImageOptions) {
      return;
    }
    const {
      index,
      preset
    } = initialImageOptions;
    const viewportType = viewportInfo.getViewportType();
    let numberOfSlices;
    if (viewportType === esm.Enums.ViewportType.STACK) {
      numberOfSlices = imageIds.length;
    } else if (viewportType === esm.Enums.ViewportType.ORTHOGRAPHIC) {
      const viewport = this.getCornerstoneViewport(viewportInfo.getViewportId());
      const imageSliceData = esm.utilities.getImageSliceDataForVolumeViewport(viewport);
      if (!imageSliceData) {
        return;
      }
      ({
        numberOfSlices
      } = imageSliceData);
    } else {
      return;
    }
    return this._getInitialImageIndex(numberOfSlices, index, preset);
  }
  _getInitialImageIndex(numberOfSlices, imageIndex, preset) {
    const lastSliceIndex = numberOfSlices - 1;
    if (imageIndex !== undefined) {
      return dist_esm.utilities.clip(imageIndex, 0, lastSliceIndex);
    }
    if (preset === utils_JumpPresets.First) {
      return 0;
    }
    if (preset === utils_JumpPresets.Last) {
      return lastSliceIndex;
    }
    if (preset === utils_JumpPresets.Middle) {
      // Note: this is a simple but yet very important formula.
      // since viewport reset works with the middle slice
      // if the below formula is not correct, on a viewport reset
      // it will jump to a different slice than the middle one which
      // was the initial slice, and we have some tools such as Crosshairs
      // which rely on a relative camera modifications and those will break.
      return lastSliceIndex % 2 === 0 ? lastSliceIndex / 2 : (lastSliceIndex + 1) / 2;
    }
    return 0;
  }
  async _setVolumeViewport(viewport, viewportData, viewportInfo, presentations) {
    // TODO: We need to overhaul the way data sources work so requests can be made
    // async. I think we should follow the image loader pattern which is async and
    // has a cache behind it.
    // The problem is that to set this volume, we need the metadata, but the request is
    // already in-flight, and the promise is not cached, so we have no way to wait for
    // it and know when it has fully arrived.
    // loadStudyMetadata(StudyInstanceUID) => Promise([instances for study])
    // loadSeriesMetadata(StudyInstanceUID, SeriesInstanceUID) => Promise([instances for series])
    // If you call loadStudyMetadata and it's not in the DicomMetadataStore cache, it should fire
    // a request through the data source?
    // (This call may or may not create sub-requests for series metadata)
    const volumeInputArray = [];
    const displaySetOptionsArray = viewportInfo.getDisplaySetOptions();
    const {
      hangingProtocolService
    } = this.servicesManager.services;
    const volumeToLoad = [];
    const displaySetInstanceUIDs = [];
    for (const [index, data] of viewportData.data.entries()) {
      const {
        volume,
        imageIds,
        displaySetInstanceUID
      } = data;
      displaySetInstanceUIDs.push(displaySetInstanceUID);
      if (!volume) {
        console.log('Volume display set not found');
        continue;
      }
      volumeToLoad.push(volume);
      const displaySetOptions = displaySetOptionsArray[index];
      const {
        volumeId
      } = volume;
      volumeInputArray.push({
        imageIds,
        volumeId,
        blendMode: displaySetOptions.blendMode,
        slabThickness: this._getSlabThickness(displaySetOptions, volumeId)
      });
    }
    this.viewportsDisplaySets.set(viewport.id, displaySetInstanceUIDs);
    if (hangingProtocolService.getShouldPerformCustomImageLoad()) {
      // delegate the volume loading to the hanging protocol service if it has a custom image load strategy
      return hangingProtocolService.runImageLoadStrategy({
        viewportId: viewport.id,
        volumeInputArray
      });
    }
    volumeToLoad.forEach(volume => {
      if (!volume.loadStatus.loaded && !volume.loadStatus.loading) {
        volume.load();
      }
    });

    // This returns the async continuation only
    return this.setVolumesForViewport(viewport, volumeInputArray, presentations);
  }
  async setVolumesForViewport(viewport, volumeInputArray, presentations) {
    const {
      displaySetService,
      toolGroupService
    } = this.servicesManager.services;
    const viewportInfo = this.getViewportInfo(viewport.id);
    const displaySetOptions = viewportInfo.getDisplaySetOptions();

    // Todo: use presentations states
    const volumesProperties = volumeInputArray.map((volumeInput, index) => {
      const {
        volumeId
      } = volumeInput;
      const displaySetOption = displaySetOptions[index];
      const {
        voi,
        voiInverted,
        colormap,
        displayPreset
      } = displaySetOption;
      const properties = {};
      if (voi && (voi.windowWidth || voi.windowCenter)) {
        const {
          lower,
          upper
        } = esm.utilities.windowLevel.toLowHighRange(voi.windowWidth, voi.windowCenter);
        properties.voiRange = {
          lower,
          upper
        };
      }
      if (voiInverted !== undefined) {
        properties.invert = voiInverted;
      }
      if (colormap !== undefined) {
        properties.colormap = colormap;
      }
      if (displayPreset !== undefined) {
        properties.preset = displayPreset;
      }
      return {
        properties,
        volumeId
      };
    });
    await viewport.setVolumes(volumeInputArray);
    volumesProperties.forEach(_ref2 => {
      let {
        properties,
        volumeId
      } = _ref2;
      viewport.setProperties(properties, volumeId);
    });
    this.setPresentations(viewport, presentations);

    // load any secondary displaySets
    const displaySetInstanceUIDs = this.viewportsDisplaySets.get(viewport.id);

    // can be SEG or RTSTRUCT for now
    const overlayDisplaySet = displaySetInstanceUIDs.map(displaySetService.getDisplaySetByUID).find(displaySet => displaySet?.isOverlayDisplaySet);
    if (overlayDisplaySet) {
      this.addOverlayRepresentationForDisplaySet(overlayDisplaySet, viewport);
    } else {
      // If the displaySet is not a SEG displaySet we assume it is a primary displaySet
      // and we can look into hydrated segmentations to check if any of them are
      // associated with the primary displaySet

      // get segmentations only returns the hydrated segmentations
      this._addSegmentationRepresentationToToolGroupIfNecessary(displaySetInstanceUIDs, viewport);
    }
    const toolGroup = toolGroupService.getToolGroupForViewport(viewport.id);
    dist_esm.utilities.segmentation.triggerSegmentationRender(toolGroup.id);
    const imageIndex = this._getInitialImageIndexForViewport(viewportInfo);
    if (imageIndex !== undefined) {
      dist_esm.utilities.jumpToSlice(viewport.element, {
        imageIndex
      });
    }
    viewport.render();
  }
  _addSegmentationRepresentationToToolGroupIfNecessary(displaySetInstanceUIDs, viewport) {
    const {
      segmentationService,
      toolGroupService
    } = this.servicesManager.services;
    const toolGroup = toolGroupService.getToolGroupForViewport(viewport.id);

    // this only returns hydrated segmentations
    const segmentations = segmentationService.getSegmentations();
    for (const segmentation of segmentations) {
      const toolGroupSegmentationRepresentations = segmentationService.getSegmentationRepresentationsForToolGroup(toolGroup.id) || [];

      // if there is already a segmentation representation for this segmentation
      // for this toolGroup, don't bother at all
      const isSegmentationInToolGroup = toolGroupSegmentationRepresentations.find(representation => representation.segmentationId === segmentation.id);
      if (isSegmentationInToolGroup) {
        continue;
      }

      // otherwise, check if the hydrated segmentations are in the same FrameOfReferenceUID
      // as the primary displaySet, if so add the representation (since it was not there)
      const {
        id: segDisplaySetInstanceUID
      } = segmentation;
      let segFrameOfReferenceUID = this._getFrameOfReferenceUID(segDisplaySetInstanceUID);
      if (!segFrameOfReferenceUID) {
        // if the segmentation displaySet does not have a FrameOfReferenceUID, we might check the
        // segmentation itself maybe it has a FrameOfReferenceUID
        const {
          FrameOfReferenceUID
        } = segmentation;
        if (FrameOfReferenceUID) {
          segFrameOfReferenceUID = FrameOfReferenceUID;
        }
      }
      if (!segFrameOfReferenceUID) {
        return;
      }
      let shouldDisplaySeg = false;
      for (const displaySetInstanceUID of displaySetInstanceUIDs) {
        const primaryFrameOfReferenceUID = this._getFrameOfReferenceUID(displaySetInstanceUID);
        if (segFrameOfReferenceUID === primaryFrameOfReferenceUID) {
          shouldDisplaySeg = true;
          break;
        }
      }
      if (!shouldDisplaySeg) {
        return;
      }
      segmentationService.addSegmentationRepresentationToToolGroup(toolGroup.id, segmentation.id, false,
      // already hydrated,
      segmentation.type);
    }
  }
  addOverlayRepresentationForDisplaySet(displaySet, viewport) {
    const {
      segmentationService,
      toolGroupService
    } = this.servicesManager.services;
    const {
      referencedVolumeId
    } = displaySet;
    const segmentationId = displaySet.displaySetInstanceUID;
    const toolGroup = toolGroupService.getToolGroupForViewport(viewport.id);
    const representationType = referencedVolumeId && esm.cache.getVolume(referencedVolumeId) !== undefined ? dist_esm.Enums.SegmentationRepresentations.Labelmap : dist_esm.Enums.SegmentationRepresentations.Contour;
    segmentationService.addSegmentationRepresentationToToolGroup(toolGroup.id, segmentationId, false, representationType);
  }

  // Todo: keepCamera is an interim solution until we have a better solution for
  // keeping the camera position when the viewport data is changed
  updateViewport(viewportId, viewportData) {
    let keepCamera = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : false;
    const viewportInfo = this.getViewportInfo(viewportId);
    const viewport = this.getCornerstoneViewport(viewportId);
    const viewportCamera = viewport.getCamera();
    if (viewport instanceof esm.VolumeViewport || viewport instanceof esm.VolumeViewport3D) {
      this._setVolumeViewport(viewport, viewportData, viewportInfo).then(() => {
        if (keepCamera) {
          viewport.setCamera(viewportCamera);
          viewport.render();
        }
      });
      return;
    }
    if (viewport instanceof esm.StackViewport) {
      this._setStackViewport(viewport, viewportData, viewportInfo);
      return;
    }
  }
  _setDisplaySets(viewport, viewportData, viewportInfo) {
    let presentations = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : {};
    if (viewport instanceof esm.StackViewport) {
      this._setStackViewport(viewport, viewportData, viewportInfo, presentations);
    } else if (viewport instanceof esm.VolumeViewport || viewport instanceof esm.VolumeViewport3D) {
      this._setVolumeViewport(viewport, viewportData, viewportInfo, presentations);
    } else {
      throw new Error('Unknown viewport type');
    }
  }

  /**
   * Removes the resize observer from the viewport element
   */
  _removeResizeObserver() {
    if (this.viewportGridResizeObserver) {
      this.viewportGridResizeObserver.disconnect();
    }
  }
  _getSlabThickness(displaySetOptions, volumeId) {
    const {
      blendMode
    } = displaySetOptions;
    if (blendMode === undefined || displaySetOptions.slabThickness === undefined) {
      return;
    }

    // if there is a slabThickness set as a number then use it
    if (typeof displaySetOptions.slabThickness === 'number') {
      return displaySetOptions.slabThickness;
    }
    if (displaySetOptions.slabThickness.toLowerCase() === 'fullvolume') {
      // calculate the slab thickness based on the volume dimensions
      const imageVolume = esm.cache.getVolume(volumeId);
      const {
        dimensions
      } = imageVolume;
      const slabThickness = Math.sqrt(dimensions[0] * dimensions[0] + dimensions[1] * dimensions[1] + dimensions[2] * dimensions[2]);
      return slabThickness;
    }
  }
  _getFrameOfReferenceUID(displaySetInstanceUID) {
    const {
      displaySetService
    } = this.servicesManager.services;
    const displaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);
    if (!displaySet) {
      return;
    }
    if (displaySet.frameOfReferenceUID) {
      return displaySet.frameOfReferenceUID;
    }
    if (displaySet.Modality === 'SEG') {
      const {
        instance
      } = displaySet;
      return instance.FrameOfReferenceUID;
    }
    if (displaySet.Modality === 'RTSTRUCT') {
      const {
        instance
      } = displaySet;
      return instance.ReferencedFrameOfReferenceSequence.FrameOfReferenceUID;
    }
    const {
      images
    } = displaySet;
    if (images && images.length) {
      return images[0].FrameOfReferenceUID;
    }
  }

  /**
   * Looks through the viewports to see if the specified measurement can be
   * displayed in one of the viewports.
   *
   * @param measurement
   *          The measurement that is desired to view.
   * @param activeViewportId - the index that was active at the time the jump
   *          was initiated.
   * @return the viewportId that the measurement should be displayed in.
   */
  getViewportIdToJump(activeViewportId, displaySetInstanceUID, cameraProps) {
    const viewportInfo = this.getViewportInfo(activeViewportId);
    const {
      referencedImageId
    } = cameraProps;
    if (viewportInfo?.contains(displaySetInstanceUID, referencedImageId)) {
      return activeViewportId;
    }
    return [...this.viewportsById.values()].find(viewportInfo => viewportInfo.contains(displaySetInstanceUID, referencedImageId))?.viewportId ?? null;
  }
}
CornerstoneViewportService.REGISTRATION = {
  name: 'cornerstoneViewportService',
  altName: 'CornerstoneViewportService',
  create: _ref3 => {
    let {
      servicesManager
    } = _ref3;
    return new CornerstoneViewportService(servicesManager);
  }
};
/* harmony default export */ const ViewportService_CornerstoneViewportService = (CornerstoneViewportService);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/types/index.ts


// EXTERNAL MODULE: ../../../node_modules/dicomweb-client/build/dicomweb-client.es.js
var dicomweb_client_es = __webpack_require__(97604);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/dicomLoaderService.js




const getImageId = imageObj => {
  if (!imageObj) {
    return;
  }
  return typeof imageObj.getImageId === 'function' ? imageObj.getImageId() : imageObj.url;
};
const findImageIdOnStudies = (studies, displaySetInstanceUID) => {
  const study = studies.find(study => {
    const displaySet = study.displaySets.some(displaySet => displaySet.displaySetInstanceUID === displaySetInstanceUID);
    return displaySet;
  });
  const {
    series = []
  } = study;
  const {
    instances = []
  } = series[0] || {};
  const instance = instances[0];
  return getImageId(instance);
};
const someInvalidStrings = strings => {
  const stringsArray = Array.isArray(strings) ? strings : [strings];
  const emptyString = string => !string;
  let invalid = stringsArray.some(emptyString);
  return invalid;
};
const getImageInstance = dataset => {
  return dataset && dataset.images && dataset.images[0];
};
const getNonImageInstance = dataset => {
  return dataset && dataset.instance;
};
const getImageInstanceId = imageInstance => {
  return getImageId(imageInstance);
};
const fetchIt = function (url) {
  let headers = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : src.DICOMWeb.getAuthorizationHeader();
  return fetch(url, headers).then(response => response.arrayBuffer());
};
const cornerstoneRetriever = imageId => {
  return esm.imageLoader.loadAndCacheImage(imageId).then(image => {
    return image && image.data && image.data.byteArray.buffer;
  });
};
const wadorsRetriever = function (url, studyInstanceUID, seriesInstanceUID, sopInstanceUID) {
  let headers = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : src.DICOMWeb.getAuthorizationHeader();
  let errorInterceptor = arguments.length > 5 && arguments[5] !== undefined ? arguments[5] : src/* errorHandler */.Po.getHTTPErrorHandler();
  const config = {
    url,
    headers,
    errorInterceptor
  };
  const dicomWeb = new dicomweb_client_es.api.DICOMwebClient(config);
  return dicomWeb.retrieveInstance({
    studyInstanceUID,
    seriesInstanceUID,
    sopInstanceUID
  });
};
const getImageLoaderType = imageId => {
  const loaderRegExp = /^\w+\:/;
  const loaderType = loaderRegExp.exec(imageId);
  return loaderRegExp.lastIndex === 0 && loaderType && loaderType[0] && loaderType[0].replace(':', '') || '';
};
class DicomLoaderService {
  getLocalData(dataset, studies) {
    // Use referenced imageInstance
    const imageInstance = getImageInstance(dataset);
    const nonImageInstance = getNonImageInstance(dataset);
    if (!imageInstance && !nonImageInstance || !nonImageInstance.imageId?.startsWith('dicomfile')) {
      return;
    }
    const instance = imageInstance || nonImageInstance;
    let imageId = getImageInstanceId(instance);

    // or Try to get it from studies
    if (someInvalidStrings(imageId)) {
      imageId = findImageIdOnStudies(studies, dataset.displaySetInstanceUID);
    }
    if (!someInvalidStrings(imageId)) {
      return cornerstoneDICOMImageLoader_min_default().wadouri.loadFileRequest(imageId);
    }
  }
  getDataByImageType(dataset) {
    const imageInstance = getImageInstance(dataset);
    if (imageInstance) {
      const imageId = getImageInstanceId(imageInstance);
      let getDicomDataMethod = fetchIt;
      const loaderType = getImageLoaderType(imageId);
      switch (loaderType) {
        case 'dicomfile':
          getDicomDataMethod = cornerstoneRetriever.bind(this, imageId);
          break;
        case 'wadors':
          const url = imageInstance.getData().wadoRoot;
          const studyInstanceUID = imageInstance.getStudyInstanceUID();
          const seriesInstanceUID = imageInstance.getSeriesInstanceUID();
          const sopInstanceUID = imageInstance.getSOPInstanceUID();
          const invalidParams = someInvalidStrings([url, studyInstanceUID, seriesInstanceUID, sopInstanceUID]);
          if (invalidParams) {
            return;
          }
          getDicomDataMethod = wadorsRetriever.bind(this, url, studyInstanceUID, seriesInstanceUID, sopInstanceUID);
          break;
        case 'wadouri':
          // Strip out the image loader specifier
          imageId = imageId.substring(imageId.indexOf(':') + 1);
          if (someInvalidStrings(imageId)) {
            return;
          }
          getDicomDataMethod = fetchIt.bind(this, imageId);
          break;
        default:
          throw new Error(`Unsupported image type: ${loaderType} for imageId: ${imageId}`);
      }
      return getDicomDataMethod();
    }
  }
  getDataByDatasetType(dataset) {
    const {
      StudyInstanceUID,
      SeriesInstanceUID,
      SOPInstanceUID,
      authorizationHeaders,
      wadoRoot,
      wadoUri
    } = dataset;
    // Retrieve wadors or just try to fetch wadouri
    if (!someInvalidStrings(wadoRoot)) {
      return wadorsRetriever(wadoRoot, StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID, authorizationHeaders);
    } else if (!someInvalidStrings(wadoUri)) {
      return fetchIt(wadoUri, {
        headers: authorizationHeaders
      });
    }
  }
  *getLoaderIterator(dataset, studies, headers) {
    yield this.getLocalData(dataset, studies);
    yield this.getDataByImageType(dataset);
    yield this.getDataByDatasetType(dataset);
  }
  findDicomDataPromise(dataset, studies, headers) {
    dataset.authorizationHeaders = headers;
    const loaderIterator = this.getLoaderIterator(dataset, studies);
    // it returns first valid retriever method.
    for (const loader of loaderIterator) {
      if (loader) {
        return loader;
      }
    }

    // in case of no valid loader
    throw new Error('Invalid dicom data loader');
  }
}
const dicomLoaderService = new DicomLoaderService();
/* harmony default export */ const utils_dicomLoaderService = (dicomLoaderService);
;// CONCATENATED MODULE: ../../../extensions/cornerstone/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-cornerstone"}');
;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/utils/measurementServiceMappings/index.ts


;// CONCATENATED MODULE: ../../../extensions/cornerstone/src/index.tsx
function src_extends() { src_extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return src_extends.apply(this, arguments); }





















const Component = /*#__PURE__*/react.lazy(() => {
  return Promise.all(/* import() */[__webpack_require__.e(23), __webpack_require__.e(181)]).then(__webpack_require__.bind(__webpack_require__, 86181));
});
const OHIFCornerstoneViewport = props => {
  return /*#__PURE__*/react.createElement(react.Suspense, {
    fallback: /*#__PURE__*/react.createElement("div", null, "Loading...")
  }, /*#__PURE__*/react.createElement(Component, props));
};

/**
 *
 */
const cornerstoneExtension = {
  /**
   * Only required property. Should be a unique value across all extensions.
   */
  id: id,
  onModeExit: () => {
    // Empty out the image load and retrieval pools to prevent memory leaks
    // on the mode exits
    Object.values(esm.Enums.RequestType).forEach(type => {
      esm.imageLoadPoolManager.clearRequestStack(type);
      esm.imageRetrievalPoolManager.clearRequestStack(type);
    });
    (0,state/* reset */.mc)();
  },
  /**
   * Register the Cornerstone 3D services and set them up for use.
   *
   * @param configuration.csToolsConfig - Passed directly to `initCornerstoneTools`
   */
  preRegistration: function (props) {
    const {
      servicesManager
    } = props;
    servicesManager.registerService(ViewportService_CornerstoneViewportService.REGISTRATION);
    servicesManager.registerService(services_ToolGroupService.REGISTRATION);
    servicesManager.registerService(services_SyncGroupService.REGISTRATION);
    servicesManager.registerService(services_SegmentationService.REGISTRATION);
    servicesManager.registerService(services_CornerstoneCacheService.REGISTRATION);
    return init.call(this, props);
  },
  getHangingProtocolModule: src_getHangingProtocolModule,
  getViewportModule(_ref) {
    let {
      servicesManager,
      commandsManager
    } = _ref;
    const ExtendedOHIFCornerstoneViewport = props => {
      // const onNewImageHandler = jumpData => {
      //   commandsManager.runCommand('jumpToImage', jumpData);
      // };
      const {
        toolbarService
      } = servicesManager.services;
      return /*#__PURE__*/react.createElement(OHIFCornerstoneViewport, src_extends({}, props, {
        toolbarService: toolbarService,
        servicesManager: servicesManager,
        commandsManager: commandsManager
      }));
    };
    return [{
      name: 'cornerstone',
      component: ExtendedOHIFCornerstoneViewport
    }];
  },
  getCommandsModule: src_commandsModule,
  getCustomizationModule: src_getCustomizationModule,
  getUtilityModule(_ref2) {
    let {
      servicesManager
    } = _ref2;
    return [{
      name: 'common',
      exports: {
        getCornerstoneLibraries: () => {
          return {
            cornerstone: esm,
            cornerstoneTools: dist_esm
          };
        },
        getEnabledElement: state/* getEnabledElement */.K8,
        dicomLoaderService: utils_dicomLoaderService
      }
    }, {
      name: 'core',
      exports: {
        Enums: esm.Enums
      }
    }, {
      name: 'tools',
      exports: {
        toolNames: toolNames,
        Enums: dist_esm.Enums
      }
    }];
  }
};

/* harmony default export */ const cornerstone_src = (cornerstoneExtension);

/***/ }),

/***/ 73704:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   K8: () => (/* binding */ getEnabledElement),
/* harmony export */   Yc: () => (/* binding */ setEnabledElement),
/* harmony export */   mc: () => (/* binding */ reset)
/* harmony export */ });
const state = {
  // The `defaultContext` of an extension's commandsModule
  DEFAULT_CONTEXT: 'CORNERSTONE',
  enabledElements: {}
};

/**
 * Sets the enabled element `dom` reference for an active viewport.
 * @param {HTMLElement} dom Active viewport element.
 * @return void
 */
const setEnabledElement = (viewportId, element, context) => {
  const targetContext = context || state.DEFAULT_CONTEXT;
  state.enabledElements[viewportId] = {
    element,
    context: targetContext
  };
};

/**
 * Grabs the enabled element `dom` reference of an active viewport.
 *
 * @return {HTMLElement} Active viewport element.
 */
const getEnabledElement = viewportId => {
  return state.enabledElements[viewportId];
};
const reset = () => {
  state.enabledElements = {};
};


/***/ }),

/***/ 87172:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   Z: () => (/* binding */ getSOPInstanceAttributes)
/* harmony export */ });
/* harmony import */ var _cornerstonejs_core__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(3743);


/**
 * It checks if the imageId is provided then it uses it to query
 * the metadata and get the SOPInstanceUID, SeriesInstanceUID and StudyInstanceUID.
 * If the imageId is not provided then undefined is returned.
 * @param {string} imageId The image id of the referenced image
 * @returns
 */
function getSOPInstanceAttributes(imageId) {
  if (imageId) {
    return _getUIDFromImageID(imageId);
  }

  // Todo: implement for volume viewports and use the referencedSeriesInstanceUID
}

function _getUIDFromImageID(imageId) {
  const instance = _cornerstonejs_core__WEBPACK_IMPORTED_MODULE_0__.metaData.get('instance', imageId);
  return {
    SOPInstanceUID: instance.SOPInstanceUID,
    SeriesInstanceUID: instance.SeriesInstanceUID,
    StudyInstanceUID: instance.StudyInstanceUID,
    frameNumber: instance.frameNumber || 1
  };
}

/***/ }),

/***/ 78753:
/***/ (() => {

/* (ignored) */

/***/ })

}]);