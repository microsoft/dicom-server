(self["webpackChunk"] = self["webpackChunk"] || []).push([[663],{

/***/ 42170:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  createReferencedImageDisplaySet: () => (/* reexport */ utils_createReferencedImageDisplaySet),
  "default": () => (/* binding */ cornerstone_dicom_sr_src),
  hydrateStructuredReport: () => (/* reexport */ hydrateStructuredReport/* default */.Z),
  srProtocol: () => (/* reexport */ srProtocol)
});

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-cornerstone-dicom-sr"}');
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/id.js

const id = package_namespaceObject.u2;
const SOPClassHandlerName = 'dicom-sr';
const SOPClassHandlerId = `${id}.sopClassHandlerModule.${SOPClassHandlerName}`;

// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../../node_modules/gl-matrix/esm/index.js + 10 modules
var esm = __webpack_require__(45451);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/tools/dist/esm/index.js + 348 modules
var dist_esm = __webpack_require__(14957);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var core_dist_esm = __webpack_require__(3743);
// EXTERNAL MODULE: ../../../extensions/cornerstone-dicom-sr/src/tools/modules/dicomSRModule.js
var dicomSRModule = __webpack_require__(64035);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/constants/scoordTypes.js
/* harmony default export */ const scoordTypes = ({
  POINT: 'POINT',
  MULTIPOINT: 'MULTIPOINT',
  POLYLINE: 'POLYLINE',
  CIRCLE: 'CIRCLE',
  ELLIPSE: 'ELLIPSE'
});
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/tools/DICOMSRDisplayTool.ts




class DICOMSRDisplayTool extends dist_esm.AnnotationTool {
  constructor() {
    let toolProps = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
    let defaultToolProps = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {
      configuration: {}
    };
    super(toolProps, defaultToolProps);
    // This tool should not inherit from AnnotationTool and we should not need
    // to add the following lines.
    this.isPointNearTool = () => null;
    this.getHandleNearImagePoint = () => null;
    this.renderAnnotation = (enabledElement, svgDrawingHelper) => {
      const {
        viewport
      } = enabledElement;
      const {
        element
      } = viewport;
      let annotations = dist_esm.annotation.state.getAnnotations(this.getToolName(), element);

      // Todo: We don't need this anymore, filtering happens in triggerAnnotationRender
      if (!annotations?.length) {
        return;
      }
      annotations = this.filterInteractableAnnotationsForElement(element, annotations);
      if (!annotations?.length) {
        return;
      }
      const trackingUniqueIdentifiersForElement = (0,dicomSRModule/* getTrackingUniqueIdentifiersForElement */.yR)(element);
      const {
        activeIndex,
        trackingUniqueIdentifiers
      } = trackingUniqueIdentifiersForElement;
      const activeTrackingUniqueIdentifier = trackingUniqueIdentifiers[activeIndex];

      // Filter toolData to only render the data for the active SR.
      const filteredAnnotations = annotations.filter(annotation => trackingUniqueIdentifiers.includes(annotation.data?.cachedStats?.TrackingUniqueIdentifier));
      if (!viewport._actors?.size) {
        return;
      }
      const styleSpecifier = {
        toolGroupId: this.toolGroupId,
        toolName: this.getToolName(),
        viewportId: enabledElement.viewport.id
      };
      for (let i = 0; i < filteredAnnotations.length; i++) {
        const annotation = filteredAnnotations[i];
        const annotationUID = annotation.annotationUID;
        const {
          renderableData
        } = annotation.data.cachedStats;
        const {
          cachedStats
        } = annotation.data;
        const {
          referencedImageId
        } = annotation.metadata;
        styleSpecifier.annotationUID = annotationUID;
        const lineWidth = this.getStyle('lineWidth', styleSpecifier, annotation);
        const lineDash = this.getStyle('lineDash', styleSpecifier, annotation);
        const color = cachedStats.TrackingUniqueIdentifier === activeTrackingUniqueIdentifier ? 'rgb(0, 255, 0)' : this.getStyle('color', styleSpecifier, annotation);
        const options = {
          color,
          lineDash,
          lineWidth
        };
        Object.keys(renderableData).forEach(GraphicType => {
          const renderableDataForGraphicType = renderableData[GraphicType];
          let renderMethod;
          let canvasCoordinatesAdapter;
          switch (GraphicType) {
            case scoordTypes.POINT:
              renderMethod = this.renderPoint;
              break;
            case scoordTypes.MULTIPOINT:
              renderMethod = this.renderMultipoint;
              break;
            case scoordTypes.POLYLINE:
              renderMethod = this.renderPolyLine;
              break;
            case scoordTypes.CIRCLE:
              renderMethod = this.renderEllipse;
              break;
            case scoordTypes.ELLIPSE:
              renderMethod = this.renderEllipse;
              canvasCoordinatesAdapter = dist_esm.utilities.math.ellipse.getCanvasEllipseCorners;
              break;
            default:
              throw new Error(`Unsupported GraphicType: ${GraphicType}`);
          }
          const canvasCoordinates = renderMethod(svgDrawingHelper, viewport, renderableDataForGraphicType, annotationUID, referencedImageId, options);
          this.renderTextBox(svgDrawingHelper, viewport, canvasCoordinates, canvasCoordinatesAdapter, annotation, styleSpecifier, options);
        });
      }
    };
  }
  _getTextBoxLinesFromLabels(labels) {
    // TODO -> max 3 for now (label + shortAxis + longAxis), need a generic solution for this!

    const labelLength = Math.min(labels.length, 3);
    const lines = [];
    for (let i = 0; i < labelLength; i++) {
      const labelEntry = labels[i];
      lines.push(`${_labelToShorthand(labelEntry.label)}${labelEntry.value}`);
    }
    return lines;
  }
  renderPolyLine(svgDrawingHelper, viewport, renderableData, annotationUID, referencedImageId, options) {
    const drawingOptions = {
      color: options.color,
      width: options.lineWidth
    };
    let allCanvasCoordinates = [];
    renderableData.map((data, index) => {
      const canvasCoordinates = data.map(p => viewport.worldToCanvas(p));
      const lineUID = `${index}`;
      if (canvasCoordinates.length === 2) {
        dist_esm.drawing.drawLine(svgDrawingHelper, annotationUID, lineUID, canvasCoordinates[0], canvasCoordinates[1], drawingOptions);
      } else {
        dist_esm.drawing.drawPolyline(svgDrawingHelper, annotationUID, lineUID, canvasCoordinates, drawingOptions);
      }
      allCanvasCoordinates = allCanvasCoordinates.concat(canvasCoordinates);
    });
    return allCanvasCoordinates; // used for drawing textBox
  }

  renderMultipoint(svgDrawingHelper, viewport, renderableData, annotationUID, referencedImageId, options) {
    let canvasCoordinates;
    renderableData.map((data, index) => {
      canvasCoordinates = data.map(p => viewport.worldToCanvas(p));
      const handleGroupUID = '0';
      dist_esm.drawing.drawHandles(svgDrawingHelper, annotationUID, handleGroupUID, canvasCoordinates, {
        color: options.color
      });
    });
  }
  renderPoint(svgDrawingHelper, viewport, renderableData, annotationUID, referencedImageId, options) {
    const canvasCoordinates = [];
    renderableData.map((data, index) => {
      const point = data[0];
      // This gives us one point for arrow
      canvasCoordinates.push(viewport.worldToCanvas(point));

      // We get the other point for the arrow by using the image size
      const imagePixelModule = core_dist_esm.metaData.get('imagePixelModule', referencedImageId);
      let xOffset = 10;
      let yOffset = 10;
      if (imagePixelModule) {
        const {
          columns,
          rows
        } = imagePixelModule;
        xOffset = columns / 10;
        yOffset = rows / 10;
      }
      const imagePoint = core_dist_esm.utilities.worldToImageCoords(referencedImageId, point);
      const arrowEnd = core_dist_esm.utilities.imageToWorldCoords(referencedImageId, [imagePoint[0] + xOffset, imagePoint[1] + yOffset]);
      canvasCoordinates.push(viewport.worldToCanvas(arrowEnd));
      const arrowUID = `${index}`;

      // Todo: handle drawing probe as probe, currently we are drawing it as an arrow
      dist_esm.drawing.drawArrow(svgDrawingHelper, annotationUID, arrowUID, canvasCoordinates[1], canvasCoordinates[0], {
        color: options.color,
        width: options.lineWidth
      });
    });
    return canvasCoordinates; // used for drawing textBox
  }

  renderEllipse(svgDrawingHelper, viewport, renderableData, annotationUID, referencedImageId, options) {
    let canvasCoordinates;
    renderableData.map((data, index) => {
      if (data.length === 0) {
        // since oblique ellipse is not supported for hydration right now
        // we just return
        return;
      }
      const ellipsePointsWorld = data;
      const rotation = viewport.getRotation();
      canvasCoordinates = ellipsePointsWorld.map(p => viewport.worldToCanvas(p));
      let canvasCorners;
      if (rotation == 90 || rotation == 270) {
        canvasCorners = dist_esm.utilities.math.ellipse.getCanvasEllipseCorners([canvasCoordinates[2], canvasCoordinates[3], canvasCoordinates[0], canvasCoordinates[1]]);
      } else {
        canvasCorners = dist_esm.utilities.math.ellipse.getCanvasEllipseCorners(canvasCoordinates);
      }
      const lineUID = `${index}`;
      dist_esm.drawing.drawEllipse(svgDrawingHelper, annotationUID, lineUID, canvasCorners[0], canvasCorners[1], {
        color: options.color,
        width: options.lineWidth
      });
    });
    return canvasCoordinates;
  }
  renderTextBox(svgDrawingHelper, viewport, canvasCoordinates, canvasCoordinatesAdapter, annotation, styleSpecifier) {
    let options = arguments.length > 6 && arguments[6] !== undefined ? arguments[6] : {};
    if (!canvasCoordinates || !annotation) {
      return;
    }
    const {
      annotationUID,
      data = {}
    } = annotation;
    const {
      label
    } = data;
    const {
      color
    } = options;
    let adaptedCanvasCoordinates = canvasCoordinates;
    // adapt coordinates if there is an adapter
    if (typeof canvasCoordinatesAdapter === 'function') {
      adaptedCanvasCoordinates = canvasCoordinatesAdapter(canvasCoordinates);
    }
    const textLines = this._getTextBoxLinesFromLabels(label);
    const canvasTextBoxCoords = dist_esm.utilities.drawing.getTextBoxCoordsCanvas(adaptedCanvasCoordinates);
    annotation.data.handles.textBox.worldPosition = viewport.canvasToWorld(canvasTextBoxCoords);
    const textBoxPosition = viewport.worldToCanvas(annotation.data.handles.textBox.worldPosition);
    const textBoxUID = '1';
    const textBoxOptions = this.getLinkedTextBoxStyle(styleSpecifier, annotation);
    const boundingBox = dist_esm.drawing.drawLinkedTextBox(svgDrawingHelper, annotationUID, textBoxUID, textLines, textBoxPosition, canvasCoordinates, {}, {
      ...textBoxOptions,
      color
    });
    const {
      x: left,
      y: top,
      width,
      height
    } = boundingBox;
    annotation.data.handles.textBox.worldBoundingBox = {
      topLeft: viewport.canvasToWorld([left, top]),
      topRight: viewport.canvasToWorld([left + width, top]),
      bottomLeft: viewport.canvasToWorld([left, top + height]),
      bottomRight: viewport.canvasToWorld([left + width, top + height])
    };
  }
}
DICOMSRDisplayTool.toolName = 'DICOMSRDisplay';
const SHORT_HAND_MAP = {
  'Short Axis': 'W: ',
  'Long Axis': 'L: ',
  AREA: 'Area: ',
  Length: '',
  CORNERSTONEFREETEXT: ''
};
function _labelToShorthand(label) {
  const shortHand = SHORT_HAND_MAP[label];
  if (shortHand !== undefined) {
    return shortHand;
  }
  return label;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/tools/toolNames.ts

const toolNames = {
  DICOMSRDisplay: DICOMSRDisplayTool.toolName,
  SRLength: 'SRLength',
  SRBidirectional: 'SRBidirectional',
  SREllipticalROI: 'SREllipticalROI',
  SRCircleROI: 'SRCircleROI',
  SRArrowAnnotate: 'SRArrowAnnotate',
  SRAngle: 'SRAngle',
  SRCobbAngle: 'SRCobbAngle',
  SRRectangleROI: 'SRRectangleROI',
  SRPlanarFreehandROI: 'SRPlanarFreehandROI'
};
/* harmony default export */ const tools_toolNames = (toolNames);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/utils/addMeasurement.ts





const EPSILON = 1e-4;
const supportedLegacyCornerstoneTags = (/* unused pure expression or super */ null && (['cornerstoneTools@^4.0.0']));
function addMeasurement(measurement, imageId, displaySetInstanceUID) {
  // TODO -> Render rotated ellipse .
  const toolName = tools_toolNames.DICOMSRDisplay;
  const measurementData = {
    TrackingUniqueIdentifier: measurement.TrackingUniqueIdentifier,
    renderableData: {},
    labels: measurement.labels,
    imageId
  };
  measurement.coords.forEach(coord => {
    const {
      GraphicType,
      GraphicData
    } = coord;
    if (measurementData.renderableData[GraphicType] === undefined) {
      measurementData.renderableData[GraphicType] = [];
    }
    measurementData.renderableData[GraphicType].push(_getRenderableData(GraphicType, GraphicData, imageId, measurement.TrackingIdentifier));
  });

  // Use the metadata provider to grab its imagePlaneModule metadata
  const imagePlaneModule = core_dist_esm.metaData.get('imagePlaneModule', imageId);
  const annotationManager = dist_esm.annotation.state.getAnnotationManager();

  // Create Cornerstone3D Annotation from measurement
  const frameNumber = measurement.coords[0].ReferencedSOPSequence && measurement.coords[0].ReferencedSOPSequence[0]?.ReferencedFrameNumber || 1;
  const SRAnnotation = {
    annotationUID: measurement.TrackingUniqueIdentifier,
    metadata: {
      FrameOfReferenceUID: imagePlaneModule.frameOfReferenceUID,
      toolName: toolName,
      referencedImageId: imageId
    },
    data: {
      label: measurement.labels,
      handles: {
        textBox: {}
      },
      cachedStats: {
        TrackingUniqueIdentifier: measurementData.TrackingUniqueIdentifier,
        renderableData: measurementData.renderableData
      },
      frameNumber: frameNumber
    }
  };
  annotationManager.addAnnotation(SRAnnotation);
  measurement.loaded = true;
  measurement.imageId = imageId;
  measurement.displaySetInstanceUID = displaySetInstanceUID;

  // Remove the unneeded coord now its processed, but keep the SOPInstanceUID.
  // NOTE: We assume that each SCOORD in the MeasurementGroup maps onto one frame,
  // It'd be super weird if it didn't anyway as a SCOORD.
  measurement.ReferencedSOPInstanceUID = measurement.coords[0].ReferencedSOPSequence.ReferencedSOPInstanceUID;
  measurement.frameNumber = frameNumber;
  delete measurement.coords;
}
function _getRenderableData(GraphicType, GraphicData, imageId, TrackingIdentifier) {
  const [cornerstoneTag, toolName] = TrackingIdentifier.split(':');
  let renderableData;
  switch (GraphicType) {
    case scoordTypes.POINT:
    case scoordTypes.MULTIPOINT:
    case scoordTypes.POLYLINE:
      renderableData = [];
      for (let i = 0; i < GraphicData.length; i += 2) {
        const worldPos = core_dist_esm.utilities.imageToWorldCoords(imageId, [GraphicData[i], GraphicData[i + 1]]);
        renderableData.push(worldPos);
      }
      break;
    case scoordTypes.CIRCLE:
      {
        const pointsWorld = [];
        for (let i = 0; i < GraphicData.length; i += 2) {
          const worldPos = core_dist_esm.utilities.imageToWorldCoords(imageId, [GraphicData[i], GraphicData[i + 1]]);
          pointsWorld.push(worldPos);
        }

        // We do not have an explicit draw circle svg helper in Cornerstone3D at
        // this time, but we can use the ellipse svg helper to draw a circle, so
        // here we reshape the data for that purpose.
        const center = pointsWorld[0];
        const onPerimeter = pointsWorld[1];
        const radius = esm/* vec3.distance */.R3.distance(center, onPerimeter);
        const imagePlaneModule = core_dist_esm.metaData.get('imagePlaneModule', imageId);
        if (!imagePlaneModule) {
          throw new Error('No imagePlaneModule found');
        }
        const {
          columnCosines,
          rowCosines
        } = imagePlaneModule;

        // we need to get major/minor axis (which are both the same size major = minor)

        // first axisStart
        const firstAxisStart = esm/* vec3.create */.R3.create();
        esm/* vec3.scaleAndAdd */.R3.scaleAndAdd(firstAxisStart, center, columnCosines, radius);
        const firstAxisEnd = esm/* vec3.create */.R3.create();
        esm/* vec3.scaleAndAdd */.R3.scaleAndAdd(firstAxisEnd, center, columnCosines, -radius);

        // second axisStart
        const secondAxisStart = esm/* vec3.create */.R3.create();
        esm/* vec3.scaleAndAdd */.R3.scaleAndAdd(secondAxisStart, center, rowCosines, radius);
        const secondAxisEnd = esm/* vec3.create */.R3.create();
        esm/* vec3.scaleAndAdd */.R3.scaleAndAdd(secondAxisEnd, center, rowCosines, -radius);
        renderableData = [firstAxisStart, firstAxisEnd, secondAxisStart, secondAxisEnd];
        break;
      }
    case scoordTypes.ELLIPSE:
      {
        // GraphicData is ordered as [majorAxisStartX, majorAxisStartY, majorAxisEndX, majorAxisEndY, minorAxisStartX, minorAxisStartY, minorAxisEndX, minorAxisEndY]
        // But Cornerstone3D points are ordered as top, bottom, left, right for the
        // ellipse so we need to identify if the majorAxis is horizontal or vertical
        // and then choose the correct points to use for the ellipse.

        const pointsWorld = [];
        for (let i = 0; i < GraphicData.length; i += 2) {
          const worldPos = core_dist_esm.utilities.imageToWorldCoords(imageId, [GraphicData[i], GraphicData[i + 1]]);
          pointsWorld.push(worldPos);
        }
        const majorAxisStart = esm/* vec3.fromValues */.R3.fromValues(...pointsWorld[0]);
        const majorAxisEnd = esm/* vec3.fromValues */.R3.fromValues(...pointsWorld[1]);
        const minorAxisStart = esm/* vec3.fromValues */.R3.fromValues(...pointsWorld[2]);
        const minorAxisEnd = esm/* vec3.fromValues */.R3.fromValues(...pointsWorld[3]);
        const majorAxisVec = esm/* vec3.create */.R3.create();
        esm/* vec3.sub */.R3.sub(majorAxisVec, majorAxisEnd, majorAxisStart);

        // normalize majorAxisVec to avoid scaling issues
        esm/* vec3.normalize */.R3.normalize(majorAxisVec, majorAxisVec);
        const minorAxisVec = esm/* vec3.create */.R3.create();
        esm/* vec3.sub */.R3.sub(minorAxisVec, minorAxisEnd, minorAxisStart);
        esm/* vec3.normalize */.R3.normalize(minorAxisVec, minorAxisVec);
        const imagePlaneModule = core_dist_esm.metaData.get('imagePlaneModule', imageId);
        if (!imagePlaneModule) {
          throw new Error('imageId does not have imagePlaneModule metadata');
        }
        const {
          columnCosines
        } = imagePlaneModule;

        // find which axis is parallel to the columnCosines
        const columnCosinesVec = esm/* vec3.fromValues */.R3.fromValues(...columnCosines);
        const projectedMajorAxisOnColVec = Math.abs(esm/* vec3.dot */.R3.dot(columnCosinesVec, majorAxisVec));
        const projectedMinorAxisOnColVec = Math.abs(esm/* vec3.dot */.R3.dot(columnCosinesVec, minorAxisVec));
        const absoluteOfMajorDotProduct = Math.abs(projectedMajorAxisOnColVec);
        const absoluteOfMinorDotProduct = Math.abs(projectedMinorAxisOnColVec);
        renderableData = [];
        if (Math.abs(absoluteOfMajorDotProduct - 1) < EPSILON) {
          renderableData = [pointsWorld[0], pointsWorld[1], pointsWorld[2], pointsWorld[3]];
        } else if (Math.abs(absoluteOfMinorDotProduct - 1) < EPSILON) {
          renderableData = [pointsWorld[2], pointsWorld[3], pointsWorld[0], pointsWorld[1]];
        } else {
          console.warn('OBLIQUE ELLIPSE NOT YET SUPPORTED');
        }
        break;
      }
    default:
      console.warn('Unsupported GraphicType:', GraphicType);
  }
  return renderableData;
}
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/adapters/dist/adapters.es.js
var adapters_es = __webpack_require__(91202);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/utils/isRehydratable.js

const cornerstoneAdapters = adapters_es.adaptersSR.Cornerstone3D.MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_UTILITY_TYPE;
const isRehydratable_supportedLegacyCornerstoneTags = ['cornerstoneTools@^4.0.0'];
const CORNERSTONE_3D_TAG = cornerstoneAdapters.CORNERSTONE_3D_TAG;

/**
 * Checks if the given `displaySet`can be rehydrated into the `measurementService`.
 *
 * @param {object} displaySet The SR `displaySet` to check.
 * @param {object[]} mappings The CornerstoneTools 4 mappings to the `measurementService`.
 * @returns {boolean} True if the SR can be rehydrated into the `measurementService`.
 */
function isRehydratable(displaySet, mappings) {
  if (!mappings || !mappings.length) {
    return false;
  }
  const mappingDefinitions = mappings.map(m => m.annotationType);
  const {
    measurements
  } = displaySet;
  const adapterKeys = Object.keys(cornerstoneAdapters).filter(adapterKey => typeof cornerstoneAdapters[adapterKey].isValidCornerstoneTrackingIdentifier === 'function');
  const adapters = [];
  adapterKeys.forEach(key => {
    if (mappingDefinitions.includes(key)) {
      // Must have both a dcmjs adapter and a measurementService
      // Definition in order to be a candidate for import.
      adapters.push(cornerstoneAdapters[key]);
    }
  });
  for (let i = 0; i < measurements.length; i++) {
    const {
      TrackingIdentifier
    } = measurements[i] || {};
    const hydratable = adapters.some(adapter => {
      let [cornerstoneTag, toolName] = TrackingIdentifier.split(':');
      if (isRehydratable_supportedLegacyCornerstoneTags.includes(cornerstoneTag)) {
        cornerstoneTag = CORNERSTONE_3D_TAG;
      }
      const mappedTrackingIdentifier = `${cornerstoneTag}:${toolName}`;
      return adapter.isValidCornerstoneTrackingIdentifier(mappedTrackingIdentifier);
    });
    if (hydratable) {
      return true;
    }
    console.log('Measurement is not rehydratable', TrackingIdentifier, measurements[i]);
  }
  console.log('No measurements found which were rehydratable');
  return false;
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/getSopClassHandlerModule.ts





const {
  CodeScheme: Cornerstone3DCodeScheme
} = adapters_es.adaptersSR.Cornerstone3D;
const {
  ImageSet,
  MetadataProvider: metadataProvider
} = src.classes;

// TODO ->
// Add SR thumbnail
// Make viewport
// Get stacks from referenced displayInstanceUID and load into wrapped CornerStone viewport.

const sopClassUids = ['1.2.840.10008.5.1.4.1.1.88.11',
//BASIC_TEXT_SR:
'1.2.840.10008.5.1.4.1.1.88.22',
//ENHANCED_SR:
'1.2.840.10008.5.1.4.1.1.88.33',
//COMPREHENSIVE_SR:
'1.2.840.10008.5.1.4.1.1.88.34' //COMPREHENSIVE_3D_SR:
];

const CORNERSTONE_3D_TOOLS_SOURCE_NAME = 'Cornerstone3DTools';
const CORNERSTONE_3D_TOOLS_SOURCE_VERSION = '0.1';
const validateSameStudyUID = (uid, instances) => {
  instances.forEach(it => {
    if (it.StudyInstanceUID !== uid) {
      console.warn('Not all instances have the same UID', uid, it);
      throw new Error(`Instances ${it.SOPInstanceUID} does not belong to ${uid}`);
    }
  });
};
const CodeNameCodeSequenceValues = {
  ImagingMeasurementReport: '126000',
  ImageLibrary: '111028',
  ImagingMeasurements: '126010',
  MeasurementGroup: '125007',
  ImageLibraryGroup: '126200',
  TrackingUniqueIdentifier: '112040',
  TrackingIdentifier: '112039',
  Finding: '121071',
  FindingSite: 'G-C0E3',
  // SRT
  CornerstoneFreeText: Cornerstone3DCodeScheme.codeValues.CORNERSTONEFREETEXT //
};

const CodingSchemeDesignators = {
  SRT: 'SRT',
  CornerstoneCodeSchemes: [Cornerstone3DCodeScheme.CodingSchemeDesignator, 'CST4']
};
const RELATIONSHIP_TYPE = {
  INFERRED_FROM: 'INFERRED FROM',
  CONTAINS: 'CONTAINS'
};
const CORNERSTONE_FREETEXT_CODE_VALUE = 'CORNERSTONEFREETEXT';

/**
 * Adds instances to the DICOM SR series, rather than creating a new
 * series, so that as SR's are saved, they append to the series, and the
 * key image display set gets updated as well, containing just the new series.
 * @param instances is a list of instances from THIS series that are not
 *     in this DICOM SR Display Set already.
 */
function addInstances(instances, displaySetService) {
  this.instances.push(...instances);
  src.utils.sortStudyInstances(this.instances);
  // The last instance is the newest one, so is the one most interesting.
  // Eventually, the SR viewer should have the ability to choose which SR
  // gets loaded, and to navigate among them.
  this.instance = this.instances[this.instances.length - 1];
  this.isLoaded = false;
  return this;
}

/**
 * DICOM SR SOP Class Handler
 * For all referenced images in the TID 1500/300 sections, add an image to the
 * display.
 * @param instances is a set of instances all from the same series
 * @param servicesManager is the services that can be used for creating
 * @returns The list of display sets created for the given instances object
 */
function _getDisplaySetsFromSeries(instances, servicesManager, extensionManager) {
  // If the series has no instances, stop here
  if (!instances || !instances.length) {
    throw new Error('No instances were provided');
  }
  src.utils.sortStudyInstances(instances);
  // The last instance is the newest one, so is the one most interesting.
  // Eventually, the SR viewer should have the ability to choose which SR
  // gets loaded, and to navigate among them.
  const instance = instances[instances.length - 1];
  const {
    StudyInstanceUID,
    SeriesInstanceUID,
    SOPInstanceUID,
    SeriesDescription,
    SeriesNumber,
    SeriesDate,
    ConceptNameCodeSequence,
    SOPClassUID
  } = instance;
  validateSameStudyUID(instance.StudyInstanceUID, instances);
  if (!ConceptNameCodeSequence || ConceptNameCodeSequence.CodeValue !== CodeNameCodeSequenceValues.ImagingMeasurementReport) {
    servicesManager.services.uiNotificationService.show({
      title: 'DICOM SR',
      message: 'OHIF only supports TID1500 Imaging Measurement Report Structured Reports. The SR you’re trying to view is not supported.',
      type: 'warning',
      duration: 6000
    });
    return [];
  }
  const displaySet = {
    //plugin: id,
    Modality: 'SR',
    displaySetInstanceUID: src.utils.guid(),
    SeriesDescription,
    SeriesNumber,
    SeriesDate,
    SOPInstanceUID,
    SeriesInstanceUID,
    StudyInstanceUID,
    SOPClassHandlerId: SOPClassHandlerId,
    SOPClassUID,
    instances,
    referencedImages: null,
    measurements: null,
    isDerivedDisplaySet: true,
    isLoaded: false,
    sopClassUids,
    instance,
    addInstances
  };
  displaySet.load = () => _load(displaySet, servicesManager, extensionManager);
  return [displaySet];
}
function _load(displaySet, servicesManager, extensionManager) {
  const {
    displaySetService,
    measurementService
  } = servicesManager.services;
  const dataSources = extensionManager.getDataSources();
  const dataSource = dataSources[0];
  const {
    ContentSequence
  } = displaySet.instance;
  displaySet.referencedImages = _getReferencedImagesList(ContentSequence);
  displaySet.measurements = _getMeasurements(ContentSequence);
  const mappings = measurementService.getSourceMappings(CORNERSTONE_3D_TOOLS_SOURCE_NAME, CORNERSTONE_3D_TOOLS_SOURCE_VERSION);
  displaySet.isHydrated = false;
  displaySet.isRehydratable = isRehydratable(displaySet, mappings);
  displaySet.isLoaded = true;

  // Check currently added displaySets and add measurements if the sources exist.
  displaySetService.activeDisplaySets.forEach(activeDisplaySet => {
    _checkIfCanAddMeasurementsToDisplaySet(displaySet, activeDisplaySet, dataSource);
  });

  // Subscribe to new displaySets as the source may come in after.
  displaySetService.subscribe(displaySetService.EVENTS.DISPLAY_SETS_ADDED, data => {
    const {
      displaySetsAdded
    } = data;
    // If there are still some measurements that have not yet been loaded into cornerstone,
    // See if we can load them onto any of the new displaySets.
    displaySetsAdded.forEach(newDisplaySet => {
      _checkIfCanAddMeasurementsToDisplaySet(displaySet, newDisplaySet, dataSource);
    });
  });
}
function _checkIfCanAddMeasurementsToDisplaySet(srDisplaySet, newDisplaySet, dataSource) {
  let unloadedMeasurements = srDisplaySet.measurements.filter(measurement => measurement.loaded === false);
  if (unloadedMeasurements.length === 0) {
    // All already loaded!
    return;
  }
  if (!newDisplaySet instanceof ImageSet) {
    // This also filters out _this_ displaySet, as it is not an ImageSet.
    return;
  }
  const {
    sopClassUids,
    images
  } = newDisplaySet;

  // Check if any have the newDisplaySet is the correct SOPClass.
  unloadedMeasurements = unloadedMeasurements.filter(measurement => measurement.coords.some(coord => sopClassUids.includes(coord.ReferencedSOPSequence.ReferencedSOPClassUID)));
  if (unloadedMeasurements.length === 0) {
    // New displaySet isn't the correct SOPClass, so can't contain the referenced images.
    return;
  }
  const SOPInstanceUIDs = [];
  unloadedMeasurements.forEach(measurement => {
    const {
      coords
    } = measurement;
    coords.forEach(coord => {
      const SOPInstanceUID = coord.ReferencedSOPSequence.ReferencedSOPInstanceUID;
      if (!SOPInstanceUIDs.includes(SOPInstanceUID)) {
        SOPInstanceUIDs.push(SOPInstanceUID);
      }
    });
  });
  const imageIdsForDisplaySet = dataSource.getImageIdsForDisplaySet(newDisplaySet);
  for (const imageId of imageIdsForDisplaySet) {
    if (!unloadedMeasurements.length) {
      // All measurements loaded.
      return;
    }
    const {
      SOPInstanceUID,
      frameNumber
    } = metadataProvider.getUIDsFromImageID(imageId);
    if (SOPInstanceUIDs.includes(SOPInstanceUID)) {
      for (let j = unloadedMeasurements.length - 1; j >= 0; j--) {
        const measurement = unloadedMeasurements[j];
        if (_measurementReferencesSOPInstanceUID(measurement, SOPInstanceUID, frameNumber)) {
          addMeasurement(measurement, imageId, newDisplaySet.displaySetInstanceUID);
          unloadedMeasurements.splice(j, 1);
        }
      }
    }
  }
}
function _measurementReferencesSOPInstanceUID(measurement, SOPInstanceUID, frameNumber) {
  const {
    coords
  } = measurement;

  // NOTE: The ReferencedFrameNumber can be multiple values according to the DICOM
  //  Standard. But for now, we will support only one ReferenceFrameNumber.
  const ReferencedFrameNumber = measurement.coords[0].ReferencedSOPSequence && measurement.coords[0].ReferencedSOPSequence[0]?.ReferencedFrameNumber || 1;
  if (frameNumber && Number(frameNumber) !== Number(ReferencedFrameNumber)) {
    return false;
  }
  for (let j = 0; j < coords.length; j++) {
    const coord = coords[j];
    const {
      ReferencedSOPInstanceUID
    } = coord.ReferencedSOPSequence;
    if (ReferencedSOPInstanceUID === SOPInstanceUID) {
      return true;
    }
  }
}
function getSopClassHandlerModule(_ref) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const getDisplaySetsFromSeries = instances => {
    return _getDisplaySetsFromSeries(instances, servicesManager, extensionManager);
  };
  return [{
    name: SOPClassHandlerName,
    sopClassUids,
    getDisplaySetsFromSeries
  }];
}
function _getMeasurements(ImagingMeasurementReportContentSequence) {
  const ImagingMeasurements = ImagingMeasurementReportContentSequence.find(item => item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.ImagingMeasurements);
  const MeasurementGroups = _getSequenceAsArray(ImagingMeasurements.ContentSequence).filter(item => item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.MeasurementGroup);
  const mergedContentSequencesByTrackingUniqueIdentifiers = _getMergedContentSequencesByTrackingUniqueIdentifiers(MeasurementGroups);
  const measurements = [];
  Object.keys(mergedContentSequencesByTrackingUniqueIdentifiers).forEach(trackingUniqueIdentifier => {
    const mergedContentSequence = mergedContentSequencesByTrackingUniqueIdentifiers[trackingUniqueIdentifier];
    const measurement = _processMeasurement(mergedContentSequence);
    if (measurement) {
      measurements.push(measurement);
    }
  });
  return measurements;
}
function _getMergedContentSequencesByTrackingUniqueIdentifiers(MeasurementGroups) {
  const mergedContentSequencesByTrackingUniqueIdentifiers = {};
  MeasurementGroups.forEach(MeasurementGroup => {
    const ContentSequence = _getSequenceAsArray(MeasurementGroup.ContentSequence);
    const TrackingUniqueIdentifierItem = ContentSequence.find(item => item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.TrackingUniqueIdentifier);
    if (!TrackingUniqueIdentifierItem) {
      console.warn('No Tracking Unique Identifier, skipping ambiguous measurement.');
    }
    const trackingUniqueIdentifier = TrackingUniqueIdentifierItem.UID;
    if (mergedContentSequencesByTrackingUniqueIdentifiers[trackingUniqueIdentifier] === undefined) {
      // Add the full ContentSequence
      mergedContentSequencesByTrackingUniqueIdentifiers[trackingUniqueIdentifier] = [...ContentSequence];
    } else {
      // Add the ContentSequence minus the tracking identifier, as we have this
      // Information in the merged ContentSequence anyway.
      ContentSequence.forEach(item => {
        if (item.ConceptNameCodeSequence.CodeValue !== CodeNameCodeSequenceValues.TrackingUniqueIdentifier) {
          mergedContentSequencesByTrackingUniqueIdentifiers[trackingUniqueIdentifier].push(item);
        }
      });
    }
  });
  return mergedContentSequencesByTrackingUniqueIdentifiers;
}
function _processMeasurement(mergedContentSequence) {
  if (mergedContentSequence.some(group => group.ValueType === 'SCOORD' || group.ValueType === 'SCOORD3D')) {
    return _processTID1410Measurement(mergedContentSequence);
  }
  return _processNonGeometricallyDefinedMeasurement(mergedContentSequence);
}
function _processTID1410Measurement(mergedContentSequence) {
  // Need to deal with TID 1410 style measurements, which will have a SCOORD or SCOORD3D at the top level,
  // And non-geometric representations where each NUM has "INFERRED FROM" SCOORD/SCOORD3D

  const graphicItem = mergedContentSequence.find(group => group.ValueType === 'SCOORD');
  const UIDREFContentItem = mergedContentSequence.find(group => group.ValueType === 'UIDREF');
  const TrackingIdentifierContentItem = mergedContentSequence.find(item => item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.TrackingIdentifier);
  if (!graphicItem) {
    console.warn(`graphic ValueType ${graphicItem.ValueType} not currently supported, skipping annotation.`);
    return;
  }
  const NUMContentItems = mergedContentSequence.filter(group => group.ValueType === 'NUM');
  const measurement = {
    loaded: false,
    labels: [],
    coords: [_getCoordsFromSCOORDOrSCOORD3D(graphicItem)],
    TrackingUniqueIdentifier: UIDREFContentItem.UID,
    TrackingIdentifier: TrackingIdentifierContentItem.TextValue
  };
  NUMContentItems.forEach(item => {
    const {
      ConceptNameCodeSequence,
      MeasuredValueSequence
    } = item;
    if (MeasuredValueSequence) {
      measurement.labels.push(_getLabelFromMeasuredValueSequence(ConceptNameCodeSequence, MeasuredValueSequence));
    }
  });
  return measurement;
}
function _processNonGeometricallyDefinedMeasurement(mergedContentSequence) {
  const NUMContentItems = mergedContentSequence.filter(group => group.ValueType === 'NUM');
  const UIDREFContentItem = mergedContentSequence.find(group => group.ValueType === 'UIDREF');
  const TrackingIdentifierContentItem = mergedContentSequence.find(item => item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.TrackingIdentifier);
  const finding = mergedContentSequence.find(item => item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.Finding);
  const findingSites = mergedContentSequence.filter(item => item.ConceptNameCodeSequence.CodingSchemeDesignator === CodingSchemeDesignators.SRT && item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.FindingSite);
  const measurement = {
    loaded: false,
    labels: [],
    coords: [],
    TrackingUniqueIdentifier: UIDREFContentItem.UID,
    TrackingIdentifier: TrackingIdentifierContentItem.TextValue
  };
  if (finding && CodingSchemeDesignators.CornerstoneCodeSchemes.includes(finding.ConceptCodeSequence.CodingSchemeDesignator) && finding.ConceptCodeSequence.CodeValue === CodeNameCodeSequenceValues.CornerstoneFreeText) {
    measurement.labels.push({
      label: CORNERSTONE_FREETEXT_CODE_VALUE,
      value: finding.ConceptCodeSequence.CodeMeaning
    });
  }

  // TODO -> Eventually hopefully support SNOMED or some proper code library, just free text for now.
  if (findingSites.length) {
    const cornerstoneFreeTextFindingSite = findingSites.find(FindingSite => CodingSchemeDesignators.CornerstoneCodeSchemes.includes(FindingSite.ConceptCodeSequence.CodingSchemeDesignator) && FindingSite.ConceptCodeSequence.CodeValue === CodeNameCodeSequenceValues.CornerstoneFreeText);
    if (cornerstoneFreeTextFindingSite) {
      measurement.labels.push({
        label: CORNERSTONE_FREETEXT_CODE_VALUE,
        value: cornerstoneFreeTextFindingSite.ConceptCodeSequence.CodeMeaning
      });
    }
  }
  NUMContentItems.forEach(item => {
    const {
      ConceptNameCodeSequence,
      ContentSequence,
      MeasuredValueSequence
    } = item;
    const {
      ValueType
    } = ContentSequence;
    if (!ValueType === 'SCOORD') {
      console.warn(`Graphic ${ValueType} not currently supported, skipping annotation.`);
      return;
    }
    const coords = _getCoordsFromSCOORDOrSCOORD3D(ContentSequence);
    if (coords) {
      measurement.coords.push(coords);
    }
    if (MeasuredValueSequence) {
      measurement.labels.push(_getLabelFromMeasuredValueSequence(ConceptNameCodeSequence, MeasuredValueSequence));
    }
  });
  return measurement;
}
function _getCoordsFromSCOORDOrSCOORD3D(item) {
  const {
    ValueType,
    RelationshipType,
    GraphicType,
    GraphicData
  } = item;
  if (!(RelationshipType == RELATIONSHIP_TYPE.INFERRED_FROM || RelationshipType == RELATIONSHIP_TYPE.CONTAINS)) {
    console.warn(`Relationshiptype === ${RelationshipType}. Cannot deal with NON TID-1400 SCOORD group with RelationshipType !== "INFERRED FROM" or "CONTAINS"`);
    return;
  }
  const coords = {
    ValueType,
    GraphicType,
    GraphicData
  };

  // ContentSequence has length of 1 as RelationshipType === 'INFERRED FROM'
  if (ValueType === 'SCOORD') {
    const {
      ReferencedSOPSequence
    } = item.ContentSequence;
    coords.ReferencedSOPSequence = ReferencedSOPSequence;
  } else if (ValueType === 'SCOORD3D') {
    const {
      ReferencedFrameOfReferenceSequence
    } = item.ContentSequence;
    coords.ReferencedFrameOfReferenceSequence = ReferencedFrameOfReferenceSequence;
  }
  return coords;
}
function _getLabelFromMeasuredValueSequence(ConceptNameCodeSequence, MeasuredValueSequence) {
  const {
    CodeMeaning
  } = ConceptNameCodeSequence;
  const {
    NumericValue,
    MeasurementUnitsCodeSequence
  } = MeasuredValueSequence;
  const {
    CodeValue
  } = MeasurementUnitsCodeSequence;
  const formatedNumericValue = NumericValue ? Number(NumericValue).toFixed(2) : '';
  return {
    label: CodeMeaning,
    value: `${formatedNumericValue} ${CodeValue}`
  }; // E.g. Long Axis: 31.0 mm
}

function _getReferencedImagesList(ImagingMeasurementReportContentSequence) {
  const ImageLibrary = ImagingMeasurementReportContentSequence.find(item => item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.ImageLibrary);
  const ImageLibraryGroup = _getSequenceAsArray(ImageLibrary.ContentSequence).find(item => item.ConceptNameCodeSequence.CodeValue === CodeNameCodeSequenceValues.ImageLibraryGroup);
  const referencedImages = [];
  _getSequenceAsArray(ImageLibraryGroup.ContentSequence).forEach(item => {
    const {
      ReferencedSOPSequence
    } = item;
    if (!ReferencedSOPSequence) {
      return;
    }
    for (const ref of _getSequenceAsArray(ReferencedSOPSequence)) {
      if (ref.ReferencedSOPClassUID) {
        const {
          ReferencedSOPClassUID,
          ReferencedSOPInstanceUID
        } = ref;
        referencedImages.push({
          ReferencedSOPClassUID,
          ReferencedSOPInstanceUID
        });
      }
    }
  });
  return referencedImages;
}
function _getSequenceAsArray(sequence) {
  if (!sequence) {
    return [];
  }
  return Array.isArray(sequence) ? sequence : [sequence];
}
/* harmony default export */ const src_getSopClassHandlerModule = (getSopClassHandlerModule);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/getHangingProtocolModule.ts
const srProtocol = {
  id: '@ohif/sr',
  // Don't store this hanging protocol as it applies to the currently active
  // display set by default
  // cacheId: null,
  name: 'SR Key Images',
  // Just apply this one when specifically listed
  protocolMatchingRules: [],
  toolGroupIds: ['default'],
  // -1 would be used to indicate active only, whereas other values are
  // the number of required priors referenced - so 0 means active with
  // 0 or more priors.
  numberOfPriorsReferenced: 0,
  // Default viewport is used to define the viewport when
  // additional viewports are added using the layout tool
  defaultViewport: {
    viewportOptions: {
      viewportType: 'stack',
      toolGroupId: 'default',
      allowUnmatchedView: true
    },
    displaySets: [{
      id: 'srDisplaySetId',
      matchedDisplaySetsIndex: -1
    }]
  },
  displaySetSelectors: {
    srDisplaySetId: {
      seriesMatchingRules: [{
        attribute: 'Modality',
        constraint: {
          equals: 'SR'
        }
      }]
    }
  },
  stages: [{
    name: 'SR Key Images',
    viewportStructure: {
      layoutType: 'grid',
      properties: {
        rows: 1,
        columns: 1
      }
    },
    viewports: [{
      viewportOptions: {
        allowUnmatchedView: true
      },
      displaySets: [{
        id: 'srDisplaySetId'
      }]
    }]
  }]
};
function getHangingProtocolModule() {
  return [{
    name: srProtocol.id,
    protocol: srProtocol
  }];
}
/* harmony default export */ const src_getHangingProtocolModule = ((/* unused pure expression or super */ null && (getHangingProtocolModule)));

;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/onModeEnter.js

function onModeEnter(_ref) {
  let {
    servicesManager
  } = _ref;
  const {
    displaySetService
  } = servicesManager.services;
  const displaySetCache = displaySetService.getDisplaySetCache();
  const srDisplaySets = [...displaySetCache.values()].filter(ds => ds.SOPClassHandlerId === SOPClassHandlerId);
  srDisplaySets.forEach(ds => {
    // New mode route, allow SRs to be hydrated again
    ds.isHydrated = false;
  });
}
// EXTERNAL MODULE: ../../../node_modules/dcmjs/build/dcmjs.es.js
var dcmjs_es = __webpack_require__(67540);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/utils/getFilteredCornerstoneToolState.ts


const {
  log
} = src["default"];
function getFilteredCornerstoneToolState(measurementData, additionalFindingTypes) {
  const filteredToolState = {};
  function addToFilteredToolState(annotation, toolType) {
    if (!annotation.metadata?.referencedImageId) {
      log.warn(`[DICOMSR] No referencedImageId found for ${toolType} ${annotation.id}`);
      return;
    }
    const imageId = annotation.metadata.referencedImageId;
    if (!filteredToolState[imageId]) {
      filteredToolState[imageId] = {};
    }
    const imageIdSpecificToolState = filteredToolState[imageId];
    if (!imageIdSpecificToolState[toolType]) {
      imageIdSpecificToolState[toolType] = {
        data: []
      };
    }
    const measurementDataI = measurementData.find(md => md.uid === annotation.annotationUID);
    const toolData = imageIdSpecificToolState[toolType].data;
    let {
      finding
    } = measurementDataI;
    const findingSites = [];

    // NOTE -> We use the CORNERSTONEJS coding schemeDesignator which we have
    // defined in the @cornerstonejs/adapters
    if (measurementDataI.label) {
      if (additionalFindingTypes.includes(toolType)) {
        finding = {
          CodeValue: 'CORNERSTONEFREETEXT',
          CodingSchemeDesignator: 'CORNERSTONEJS',
          CodeMeaning: measurementDataI.label
        };
      } else {
        findingSites.push({
          CodeValue: 'CORNERSTONEFREETEXT',
          CodingSchemeDesignator: 'CORNERSTONEJS',
          CodeMeaning: measurementDataI.label
        });
      }
    }
    if (measurementDataI.findingSites) {
      findingSites.push(...measurementDataI.findingSites);
    }
    const measurement = Object.assign({}, annotation, {
      finding,
      findingSites
    });
    toolData.push(measurement);
  }
  const uidFilter = measurementData.map(md => md.uid);
  const uids = uidFilter.slice();
  const annotationManager = dist_esm.annotation.state.getAnnotationManager();
  const framesOfReference = annotationManager.getFramesOfReference();
  for (let i = 0; i < framesOfReference.length; i++) {
    const frameOfReference = framesOfReference[i];
    const frameOfReferenceAnnotations = annotationManager.getAnnotations(frameOfReference);
    const toolTypes = Object.keys(frameOfReferenceAnnotations);
    for (let j = 0; j < toolTypes.length; j++) {
      const toolType = toolTypes[j];
      const annotations = frameOfReferenceAnnotations[toolType];
      if (annotations) {
        for (let k = 0; k < annotations.length; k++) {
          const annotation = annotations[k];
          const uidIndex = uids.findIndex(uid => uid === annotation.annotationUID);
          if (uidIndex !== -1) {
            addToFilteredToolState(annotation, toolType);
            uids.splice(uidIndex, 1);
            if (!uids.length) {
              return filteredToolState;
            }
          }
        }
      }
    }
  }
  return filteredToolState;
}
/* harmony default export */ const utils_getFilteredCornerstoneToolState = (getFilteredCornerstoneToolState);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/commandsModule.js





const {
  MeasurementReport
} = adapters_es.adaptersSR.Cornerstone3D;
const {
  log: commandsModule_log
} = src["default"];

/**
 *
 * @param measurementData An array of measurements from the measurements service
 * that you wish to serialize.
 * @param additionalFindingTypes toolTypes that should be stored with labels as Findings
 * @param options Naturalized DICOM JSON headers to merge into the displaySet.
 *
 */
const _generateReport = function (measurementData, additionalFindingTypes) {
  let options = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
  const filteredToolState = utils_getFilteredCornerstoneToolState(measurementData, additionalFindingTypes);
  const report = MeasurementReport.generateReport(filteredToolState, core_dist_esm.metaData, core_dist_esm.utilities.worldToImageCoords, options);
  const {
    dataset
  } = report;

  // Set the default character set as UTF-8
  // https://dicom.innolitics.com/ciods/nm-image/sop-common/00080005
  if (typeof dataset.SpecificCharacterSet === 'undefined') {
    dataset.SpecificCharacterSet = 'ISO_IR 192';
  }
  return dataset;
};
const commandsModule = _ref => {
  let {} = _ref;
  const actions = {
    /**
     *
     * @param measurementData An array of measurements from the measurements service
     * @param additionalFindingTypes toolTypes that should be stored with labels as Findings
     * @param options Naturalized DICOM JSON headers to merge into the displaySet.
     * as opposed to Finding Sites.
     * that you wish to serialize.
     */
    downloadReport: _ref2 => {
      let {
        measurementData,
        additionalFindingTypes,
        options = {}
      } = _ref2;
      const srDataset = actions.generateReport(measurementData, additionalFindingTypes, options);
      const reportBlob = dcmjs_es["default"].data.datasetToBlob(srDataset);

      //Create a URL for the binary.
      var objectUrl = URL.createObjectURL(reportBlob);
      window.location.assign(objectUrl);
    },
    /**
     *
     * @param measurementData An array of measurements from the measurements service
     * that you wish to serialize.
     * @param dataSource The dataSource that you wish to use to persist the data.
     * @param additionalFindingTypes toolTypes that should be stored with labels as Findings
     * @param options Naturalized DICOM JSON headers to merge into the displaySet.
     * @return The naturalized report
     */
    storeMeasurements: async _ref3 => {
      let {
        measurementData,
        dataSource,
        additionalFindingTypes,
        options = {}
      } = _ref3;
      // Use the @cornerstonejs adapter for converting to/from DICOM
      // But it is good enough for now whilst we only have cornerstone as a datasource.
      commandsModule_log.info('[DICOMSR] storeMeasurements');
      if (!dataSource || !dataSource.store || !dataSource.store.dicom) {
        commandsModule_log.error('[DICOMSR] datasource has no dataSource.store.dicom endpoint!');
        return Promise.reject({});
      }
      try {
        const naturalizedReport = _generateReport(measurementData, additionalFindingTypes, options);
        const {
          StudyInstanceUID,
          ContentSequence
        } = naturalizedReport;
        // The content sequence has 5 or more elements, of which
        // the `[4]` element contains the annotation data, so this is
        // checking that there is some annotation data present.
        if (!ContentSequence?.[4].ContentSequence?.length) {
          console.log('naturalizedReport missing imaging content', naturalizedReport);
          throw new Error('Invalid report, no content');
        }
        await dataSource.store.dicom(naturalizedReport);
        if (StudyInstanceUID) {
          dataSource.deleteStudyMetadataPromise(StudyInstanceUID);
        }

        // The "Mode" route listens for DicomMetadataStore changes
        // When a new instance is added, it listens and
        // automatically calls makeDisplaySets
        src.DicomMetadataStore.addInstances([naturalizedReport], true);
        return naturalizedReport;
      } catch (error) {
        console.warn(error);
        commandsModule_log.error(`[DICOMSR] Error while saving the measurements: ${error.message}`);
        throw new Error(error.message || 'Error while saving the measurements.');
      }
    }
  };
  const definitions = {
    downloadReport: {
      commandFn: actions.downloadReport,
      storeContexts: [],
      options: {}
    },
    storeMeasurements: {
      commandFn: actions.storeMeasurements,
      storeContexts: [],
      options: {}
    }
  };
  return {
    actions,
    definitions,
    defaultContext: 'CORNERSTONE_STRUCTURED_REPORT'
  };
};
/* harmony default export */ const src_commandsModule = (commandsModule);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/utils/addToolInstance.ts

function addToolInstance(name, toolClass, configuration) {
  class InstanceClass extends toolClass {}
  InstanceClass.toolName = name;
  (0,dist_esm.addTool)(InstanceClass);
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/init.ts





/**
 * @param {object} configuration
 */
function init(_ref) {
  let {
    configuration = {}
  } = _ref;
  (0,dist_esm.addTool)(DICOMSRDisplayTool);
  addToolInstance(tools_toolNames.SRLength, dist_esm.LengthTool, {});
  addToolInstance(tools_toolNames.SRBidirectional, dist_esm.BidirectionalTool);
  addToolInstance(tools_toolNames.SREllipticalROI, dist_esm.EllipticalROITool);
  addToolInstance(tools_toolNames.SRCircleROI, dist_esm.CircleROITool);
  addToolInstance(tools_toolNames.SRArrowAnnotate, dist_esm.ArrowAnnotateTool);
  addToolInstance(tools_toolNames.SRAngle, dist_esm.AngleTool);
  // TODO - fix the SR display of Cobb Angle, as it joins the two lines
  addToolInstance(tools_toolNames.SRCobbAngle, dist_esm.CobbAngleTool);
  // TODO - fix the rehydration of Freehand, as it throws an exception
  // on a missing polyline. The fix is probably in CS3D
  addToolInstance(tools_toolNames.SRPlanarFreehandROI, dist_esm.PlanarFreehandROITool);

  // Modify annotation tools to use dashed lines on SR
  const dashedLine = {
    lineDash: '4,4'
  };
  dist_esm.annotation.config.style.setToolGroupToolStyles('SRToolGroup', {
    SRLength: dashedLine,
    SRBidirectional: dashedLine,
    SREllipticalROI: dashedLine,
    SRCircleROI: dashedLine,
    SRArrowAnnotate: dashedLine,
    SRCobbAngle: dashedLine,
    SRAngle: dashedLine,
    SRPlanarFreehandROI: dashedLine,
    global: {}
  });
}
// EXTERNAL MODULE: ../../../extensions/cornerstone-dicom-sr/src/utils/hydrateStructuredReport.js + 1 modules
var hydrateStructuredReport = __webpack_require__(38965);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/utils/createReferencedImageDisplaySet.ts

const createReferencedImageDisplaySet_ImageSet = src.classes.ImageSet;
const findInstance = (measurement, displaySetService) => {
  const {
    displaySetInstanceUID,
    ReferencedSOPInstanceUID: sopUid
  } = measurement;
  const referencedDisplaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);
  if (!referencedDisplaySet.images) {
    return;
  }
  return referencedDisplaySet.images.find(it => it.SOPInstanceUID === sopUid);
};

/** Finds references to display sets inside the measurements
 * contained within the provided display set.
 * @return an array of instances referenced.
 */
const findReferencedInstances = (displaySetService, displaySet) => {
  const instances = [];
  const instanceById = {};
  for (const measurement of displaySet.measurements) {
    const {
      imageId
    } = measurement;
    if (!imageId) {
      continue;
    }
    if (instanceById[imageId]) {
      continue;
    }
    const instance = findInstance(measurement, displaySetService);
    if (!instance) {
      console.log('Measurement', measurement, 'had no instances found');
      continue;
    }
    instanceById[imageId] = instance;
    instances.push(instance);
  }
  return instances;
};

/**
 * Creates a new display set containing a single image instance for each
 * referenced image.
 *
 * @param displaySetService
 * @param displaySet - containing measurements referencing images.
 * @returns A new (registered/active) display set containing the referenced images
 */
const createReferencedImageDisplaySet = (displaySetService, displaySet) => {
  const instances = findReferencedInstances(displaySetService, displaySet);
  // This will be a  member function of the created image set
  const updateInstances = function () {
    this.images.splice(0, this.images.length, ...findReferencedInstances(displaySetService, displaySet));
    this.numImageFrames = this.images.length;
  };
  const imageSet = new createReferencedImageDisplaySet_ImageSet(instances);
  const instance = instances[0];
  imageSet.setAttributes({
    displaySetInstanceUID: imageSet.uid,
    // create a local alias for the imageSet UID
    SeriesDate: instance.SeriesDate,
    SeriesTime: instance.SeriesTime,
    SeriesInstanceUID: imageSet.uid,
    StudyInstanceUID: instance.StudyInstanceUID,
    SeriesNumber: instance.SeriesNumber || 0,
    SOPClassUID: instance.SOPClassUID,
    SeriesDescription: `${displaySet.SeriesDescription} KO ${displaySet.instance.SeriesNumber}`,
    Modality: 'KO',
    isMultiFrame: false,
    numImageFrames: instances.length,
    SOPClassHandlerId: `@ohif/extension-default.sopClassHandlerModule.stack`,
    isReconstructable: false,
    // This object is made of multiple instances from other series
    isCompositeStack: true,
    madeInClient: true,
    excludeFromThumbnailBrowser: true,
    updateInstances
  });
  displaySetService.addDisplaySets(imageSet);
  return imageSet;
};
/* harmony default export */ const utils_createReferencedImageDisplaySet = (createReferencedImageDisplaySet);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/index.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }










const Component = /*#__PURE__*/react.lazy(() => {
  return __webpack_require__.e(/* import() */ 886).then(__webpack_require__.bind(__webpack_require__, 48886));
});
const OHIFCornerstoneSRViewport = props => {
  return /*#__PURE__*/react.createElement(react.Suspense, {
    fallback: /*#__PURE__*/react.createElement("div", null, "Loading...")
  }, /*#__PURE__*/react.createElement(Component, props));
};

/**
 *
 */
const dicomSRExtension = {
  /**
   * Only required property. Should be a unique value across all extensions.
   */
  id: id,
  onModeEnter: onModeEnter,
  preRegistration: init,
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
    const ExtendedOHIFCornerstoneSRViewport = props => {
      return /*#__PURE__*/react.createElement(OHIFCornerstoneSRViewport, _extends({
        servicesManager: servicesManager,
        extensionManager: extensionManager
      }, props));
    };
    return [{
      name: 'dicom-sr',
      component: ExtendedOHIFCornerstoneSRViewport
    }];
  },
  getCommandsModule: src_commandsModule,
  getSopClassHandlerModule: src_getSopClassHandlerModule,
  // Include dynamically computed values such as toolNames not known till instantiation
  getUtilityModule(_ref2) {
    let {
      servicesManager
    } = _ref2;
    return [{
      name: 'tools',
      exports: {
        toolNames: tools_toolNames
      }
    }];
  }
};
/* harmony default export */ const cornerstone_dicom_sr_src = (dicomSRExtension);

// Put static exports here so they can be type checked


/***/ }),

/***/ 64035:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   l2: () => (/* binding */ setTrackingUniqueIdentifiersForElement),
/* harmony export */   yR: () => (/* binding */ getTrackingUniqueIdentifiersForElement)
/* harmony export */ });
/* unused harmony export setActiveTrackingUniqueIdentifierForElement */
/* harmony import */ var _cornerstonejs_core__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(3743);

const state = {
  TrackingUniqueIdentifier: null,
  trackingIdentifiersByViewportId: {}
};

/**
 * This file is being used to store the per-viewport state of the SR tools,
 * Since, all the toolStates are added to the cornerstoneTools, when displaying the SRTools,
 * if there are two viewports rendering the same imageId, we don't want to show
 * the same SR annotation twice on irrelevant viewport, hence, we are storing the state
 * of the SR tools in state here, so that we can filter them later.
 */

function setTrackingUniqueIdentifiersForElement(element, trackingUniqueIdentifiers) {
  let activeIndex = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : 0;
  const enabledElement = (0,_cornerstonejs_core__WEBPACK_IMPORTED_MODULE_0__.getEnabledElement)(element);
  const {
    viewport
  } = enabledElement;
  state.trackingIdentifiersByViewportId[viewport.id] = {
    trackingUniqueIdentifiers,
    activeIndex
  };
}
function setActiveTrackingUniqueIdentifierForElement(element, TrackingUniqueIdentifier) {
  const enabledElement = getEnabledElement(element);
  const {
    viewport
  } = enabledElement;
  const trackingIdentifiersForElement = state.trackingIdentifiersByViewportId[viewport.id];
  if (trackingIdentifiersForElement) {
    const activeIndex = trackingIdentifiersForElement.trackingUniqueIdentifiers.findIndex(tuid => tuid === TrackingUniqueIdentifier);
    trackingIdentifiersForElement.activeIndex = activeIndex;
  }
}
function getTrackingUniqueIdentifiersForElement(element) {
  const enabledElement = (0,_cornerstonejs_core__WEBPACK_IMPORTED_MODULE_0__.getEnabledElement)(element);
  const {
    viewport
  } = enabledElement;
  if (state.trackingIdentifiersByViewportId[viewport.id]) {
    return state.trackingIdentifiersByViewportId[viewport.id];
  }
  return {
    trackingUniqueIdentifiers: []
  };
}


/***/ }),

/***/ 38965:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  Z: () => (/* binding */ hydrateStructuredReport)
});

// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var esm = __webpack_require__(3743);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/utils/getLabelFromDCMJSImportedToolData.js
/**
 * Extracts the label from the toolData imported from dcmjs. We need to do this
 * as dcmjs does not depeend on OHIF/the measurementService, it just produces data for cornestoneTools.
 * This optional data is available for the consumer to process if they wish to.
 * @param {object} toolData The tooldata relating to the
 *
 * @returns {string} The extracted label.
 */
function getLabelFromDCMJSImportedToolData(toolData) {
  const {
    findingSites = [],
    finding
  } = toolData;
  let freeTextLabel = findingSites.find(fs => fs.CodeValue === 'CORNERSTONEFREETEXT');
  if (freeTextLabel) {
    return freeTextLabel.CodeMeaning;
  }
  if (finding && finding.CodeValue === 'CORNERSTONEFREETEXT') {
    return finding.CodeMeaning;
  }
}
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/adapters/dist/adapters.es.js
var adapters_es = __webpack_require__(91202);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/tools/dist/esm/index.js + 348 modules
var dist_esm = __webpack_require__(14957);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-sr/src/utils/hydrateStructuredReport.js





const {
  locking
} = dist_esm.annotation;
const {
  guid
} = src["default"].utils;
const {
  MeasurementReport,
  CORNERSTONE_3D_TAG
} = adapters_es.adaptersSR.Cornerstone3D;
const CORNERSTONE_3D_TOOLS_SOURCE_NAME = 'Cornerstone3DTools';
const CORNERSTONE_3D_TOOLS_SOURCE_VERSION = '0.1';
const supportedLegacyCornerstoneTags = ['cornerstoneTools@^4.0.0'];
const convertCode = (codingValues, code) => {
  if (!code || code.CodingSchemeDesignator === 'CORNERSTONEJS') {
    return;
  }
  const ref = `${code.CodingSchemeDesignator}:${code.CodeValue}`;
  const ret = {
    ...codingValues[ref],
    ref,
    ...code,
    text: code.CodeMeaning
  };
  return ret;
};
const convertSites = (codingValues, sites) => {
  if (!sites || !sites.length) {
    return;
  }
  const ret = [];
  // Do as a loop to convert away from Proxy instances
  for (let i = 0; i < sites.length; i++) {
    // Deal with irregular conversion from dcmjs
    const site = convertCode(codingValues, sites[i][0] || sites[i]);
    if (site) {
      ret.push(site);
    }
  }
  return ret.length && ret || undefined;
};

/**
 * Hydrates a structured report, for default viewports.
 *
 */
function hydrateStructuredReport(_ref, displaySetInstanceUID) {
  let {
    servicesManager,
    extensionManager,
    appConfig
  } = _ref;
  const annotationManager = dist_esm.annotation.state.getAnnotationManager();
  const disableEditing = appConfig?.disableEditing;
  const dataSource = extensionManager.getActiveDataSource()[0];
  const {
    measurementService,
    displaySetService,
    customizationService
  } = servicesManager.services;
  const codingValues = customizationService.getCustomization('codingValues', {});
  const displaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);

  // TODO -> We should define a strict versioning somewhere.
  const mappings = measurementService.getSourceMappings(CORNERSTONE_3D_TOOLS_SOURCE_NAME, CORNERSTONE_3D_TOOLS_SOURCE_VERSION);
  if (!mappings || !mappings.length) {
    throw new Error(`Attempting to hydrate measurements service when no mappings present. This shouldn't be reached.`);
  }
  const instance = src.DicomMetadataStore.getInstance(displaySet.StudyInstanceUID, displaySet.SeriesInstanceUID, displaySet.SOPInstanceUID);
  const sopInstanceUIDToImageId = {};
  const imageIdsForToolState = {};
  displaySet.measurements.forEach(measurement => {
    const {
      ReferencedSOPInstanceUID,
      imageId,
      frameNumber
    } = measurement;
    if (!sopInstanceUIDToImageId[ReferencedSOPInstanceUID]) {
      sopInstanceUIDToImageId[ReferencedSOPInstanceUID] = imageId;
      imageIdsForToolState[ReferencedSOPInstanceUID] = [];
    }
    if (!imageIdsForToolState[ReferencedSOPInstanceUID][frameNumber]) {
      imageIdsForToolState[ReferencedSOPInstanceUID][frameNumber] = imageId;
    }
  });
  const datasetToUse = _mapLegacyDataSet(instance);

  // Use dcmjs to generate toolState.
  const storedMeasurementByAnnotationType = MeasurementReport.generateToolState(datasetToUse,
  // NOTE: we need to pass in the imageIds to dcmjs since the we use them
  // for the imageToWorld transformation. The following assumes that the order
  // that measurements were added to the display set are the same order as
  // the measurementGroups in the instance.
  sopInstanceUIDToImageId, esm.utilities.imageToWorldCoords, esm.metaData);

  // Filter what is found by DICOM SR to measurements we support.
  const mappingDefinitions = mappings.map(m => m.annotationType);
  const hydratableMeasurementsInSR = {};
  Object.keys(storedMeasurementByAnnotationType).forEach(key => {
    if (mappingDefinitions.includes(key)) {
      hydratableMeasurementsInSR[key] = storedMeasurementByAnnotationType[key];
    }
  });

  // Set the series touched as tracked.
  const imageIds = [];

  // TODO: notification if no hydratable?
  Object.keys(hydratableMeasurementsInSR).forEach(annotationType => {
    const toolDataForAnnotationType = hydratableMeasurementsInSR[annotationType];
    toolDataForAnnotationType.forEach(toolData => {
      // Add the measurement to toolState
      // dcmjs and Cornerstone3D has structural defect in supporting multi-frame
      // files, and looking up the imageId from sopInstanceUIDToImageId results
      // in the wrong value.
      const frameNumber = toolData.annotation.data && toolData.annotation.data.frameNumber || 1;
      const imageId = imageIdsForToolState[toolData.sopInstanceUid][frameNumber] || sopInstanceUIDToImageId[toolData.sopInstanceUid];
      if (!imageIds.includes(imageId)) {
        imageIds.push(imageId);
      }
    });
  });
  let targetStudyInstanceUID;
  const SeriesInstanceUIDs = [];
  for (let i = 0; i < imageIds.length; i++) {
    const imageId = imageIds[i];
    const {
      SeriesInstanceUID,
      StudyInstanceUID
    } = esm.metaData.get('instance', imageId);
    if (!SeriesInstanceUIDs.includes(SeriesInstanceUID)) {
      SeriesInstanceUIDs.push(SeriesInstanceUID);
    }
    if (!targetStudyInstanceUID) {
      targetStudyInstanceUID = StudyInstanceUID;
    } else if (targetStudyInstanceUID !== StudyInstanceUID) {
      console.warn('NO SUPPORT FOR SRs THAT HAVE MEASUREMENTS FROM MULTIPLE STUDIES.');
    }
  }
  Object.keys(hydratableMeasurementsInSR).forEach(annotationType => {
    const toolDataForAnnotationType = hydratableMeasurementsInSR[annotationType];
    toolDataForAnnotationType.forEach(toolData => {
      // Add the measurement to toolState
      // dcmjs and Cornerstone3D has structural defect in supporting multi-frame
      // files, and looking up the imageId from sopInstanceUIDToImageId results
      // in the wrong value.
      const frameNumber = toolData.annotation.data && toolData.annotation.data.frameNumber || 1;
      const imageId = imageIdsForToolState[toolData.sopInstanceUid][frameNumber] || sopInstanceUIDToImageId[toolData.sopInstanceUid];
      toolData.uid = guid();
      const instance = esm.metaData.get('instance', imageId);
      const {
        FrameOfReferenceUID
        // SOPInstanceUID,
        // SeriesInstanceUID,
        // StudyInstanceUID,
      } = instance;
      const annotation = {
        annotationUID: toolData.annotation.annotationUID,
        data: toolData.annotation.data,
        metadata: {
          toolName: annotationType,
          referencedImageId: imageId,
          FrameOfReferenceUID
        }
      };
      const source = measurementService.getSource(CORNERSTONE_3D_TOOLS_SOURCE_NAME, CORNERSTONE_3D_TOOLS_SOURCE_VERSION);
      annotation.data.label = getLabelFromDCMJSImportedToolData(toolData);
      annotation.data.finding = convertCode(codingValues, toolData.finding?.[0]);
      annotation.data.findingSites = convertSites(codingValues, toolData.findingSites);
      annotation.data.site = annotation.data.findingSites?.[0];
      const matchingMapping = mappings.find(m => m.annotationType === annotationType);
      const newAnnotationUID = measurementService.addRawMeasurement(source, annotationType, {
        annotation
      }, matchingMapping.toMeasurementSchema, dataSource);
      if (disableEditing) {
        const addedAnnotation = annotationManager.getAnnotation(newAnnotationUID);
        locking.setAnnotationLocked(addedAnnotation, true);
      }
      if (!imageIds.includes(imageId)) {
        imageIds.push(imageId);
      }
    });
  });
  displaySet.isHydrated = true;
  return {
    StudyInstanceUID: targetStudyInstanceUID,
    SeriesInstanceUIDs
  };
}
function _mapLegacyDataSet(dataset) {
  const REPORT = 'Imaging Measurements';
  const GROUP = 'Measurement Group';
  const TRACKING_IDENTIFIER = 'Tracking Identifier';

  // Identify the Imaging Measurements
  const imagingMeasurementContent = toArray(dataset.ContentSequence).find(codeMeaningEquals(REPORT));

  // Retrieve the Measurements themselves
  const measurementGroups = toArray(imagingMeasurementContent.ContentSequence).filter(codeMeaningEquals(GROUP));

  // For each of the supported measurement types, compute the measurement data
  const measurementData = {};
  const cornerstoneToolClasses = MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_UTILITY_TYPE;
  const registeredToolClasses = [];
  Object.keys(cornerstoneToolClasses).forEach(key => {
    registeredToolClasses.push(cornerstoneToolClasses[key]);
    measurementData[key] = [];
  });
  measurementGroups.forEach((measurementGroup, index) => {
    const measurementGroupContentSequence = toArray(measurementGroup.ContentSequence);
    const TrackingIdentifierGroup = measurementGroupContentSequence.find(contentItem => contentItem.ConceptNameCodeSequence.CodeMeaning === TRACKING_IDENTIFIER);
    const TrackingIdentifier = TrackingIdentifierGroup.TextValue;
    let [cornerstoneTag, toolName] = TrackingIdentifier.split(':');
    if (supportedLegacyCornerstoneTags.includes(cornerstoneTag)) {
      cornerstoneTag = CORNERSTONE_3D_TAG;
    }
    const mappedTrackingIdentifier = `${cornerstoneTag}:${toolName}`;
    TrackingIdentifierGroup.TextValue = mappedTrackingIdentifier;
  });
  return dataset;
}
const toArray = function (x) {
  return Array.isArray(x) ? x : [x];
};
const codeMeaningEquals = codeMeaningName => {
  return contentItem => {
    return contentItem.ConceptNameCodeSequence.CodeMeaning === codeMeaningName;
  };
};

/***/ }),

/***/ 78753:
/***/ (() => {

/* (ignored) */

/***/ })

}]);