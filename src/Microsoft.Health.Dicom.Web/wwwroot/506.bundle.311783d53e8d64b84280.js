"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[506],{

/***/ 53506:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ cornerstone_dicom_rt_src)
});

;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-cornerstone-dicom-rt"}');
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/src/id.js

const id = package_namespaceObject.u2;
const SOPClassHandlerName = 'dicom-rt';
const SOPClassHandlerId = `${id}.sopClassHandlerModule.${SOPClassHandlerName}`;

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../../node_modules/dcmjs/build/dcmjs.es.js
var dcmjs_es = __webpack_require__(67540);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/src/loadRTStruct.js

const {
  DicomMessage,
  DicomMetaDictionary
} = dcmjs_es["default"].data;
const dicomlab2RGB = dcmjs_es["default"].data.Colors.dicomlab2RGB;
async function checkAndLoadContourData(instance, datasource) {
  if (!instance || !instance.ROIContourSequence) {
    return Promise.reject('Invalid instance object or ROIContourSequence');
  }
  const promisesMap = new Map();
  for (const ROIContour of instance.ROIContourSequence) {
    const referencedROINumber = ROIContour.ReferencedROINumber;
    if (!ROIContour || !ROIContour.ContourSequence) {
      promisesMap.set(referencedROINumber, [Promise.resolve([])]);
      continue;
    }
    for (const Contour of ROIContour.ContourSequence) {
      if (!Contour || !Contour.ContourData) {
        return Promise.reject('Invalid Contour or ContourData');
      }
      const contourData = Contour.ContourData;
      if (Array.isArray(contourData)) {
        promisesMap.has(referencedROINumber) ? promisesMap.get(referencedROINumber).push(Promise.resolve(contourData)) : promisesMap.set(referencedROINumber, [Promise.resolve(contourData)]);
      } else if (contourData && contourData.BulkDataURI) {
        const bulkDataURI = contourData.BulkDataURI;
        if (!datasource || !datasource.retrieve || !datasource.retrieve.bulkDataURI) {
          return Promise.reject('Invalid datasource object or retrieve function');
        }
        const bulkDataPromise = datasource.retrieve.bulkDataURI({
          BulkDataURI: bulkDataURI,
          StudyInstanceUID: instance.StudyInstanceUID,
          SeriesInstanceUID: instance.SeriesInstanceUID,
          SOPInstanceUID: instance.SOPInstanceUID
        });
        promisesMap.has(referencedROINumber) ? promisesMap.get(referencedROINumber).push(bulkDataPromise) : promisesMap.set(referencedROINumber, [bulkDataPromise]);
      } else {
        return Promise.reject(`Invalid ContourData: ${contourData}`);
      }
    }
  }
  const resolvedPromisesMap = new Map();
  for (const [key, promiseArray] of promisesMap.entries()) {
    resolvedPromisesMap.set(key, await Promise.allSettled(promiseArray));
  }
  instance.ROIContourSequence.forEach(ROIContour => {
    try {
      const referencedROINumber = ROIContour.ReferencedROINumber;
      const resolvedPromises = resolvedPromisesMap.get(referencedROINumber);
      if (ROIContour.ContourSequence) {
        ROIContour.ContourSequence.forEach((Contour, index) => {
          const promise = resolvedPromises[index];
          if (promise.status === 'fulfilled') {
            if (Array.isArray(promise.value) && promise.value.every(Number.isFinite)) {
              // If promise.value is already an array of numbers, use it directly
              Contour.ContourData = promise.value;
            } else {
              // If the resolved promise value is a byte array (Blob), it needs to be decoded
              const uint8Array = new Uint8Array(promise.value);
              const textDecoder = new TextDecoder();
              const dataUint8Array = textDecoder.decode(uint8Array);
              if (typeof dataUint8Array === 'string' && dataUint8Array.includes('\\')) {
                Contour.ContourData = dataUint8Array.split('\\').map(parseFloat);
              } else {
                Contour.ContourData = [];
              }
            }
          } else {
            console.error(promise.reason);
          }
        });
      }
    } catch (error) {
      console.error(error);
    }
  });
}
async function loadRTStruct(extensionManager, rtStructDisplaySet, referencedDisplaySet, headers) {
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.common');
  const dataSource = extensionManager.getActiveDataSource()[0];
  const {
    bulkDataURI
  } = dataSource.getConfig?.() || {};
  const {
    dicomLoaderService
  } = utilityModule.exports;
  const imageIdSopInstanceUidPairs = _getImageIdSopInstanceUidPairsForDisplaySet(referencedDisplaySet);

  // Set here is loading is asynchronous.
  // If this function throws its set back to false.
  rtStructDisplaySet.isLoaded = true;
  let instance = rtStructDisplaySet.instance;
  if (!bulkDataURI || !bulkDataURI.enabled) {
    const segArrayBuffer = await dicomLoaderService.findDicomDataPromise(rtStructDisplaySet, null, headers);
    const dicomData = DicomMessage.readFile(segArrayBuffer);
    const rtStructDataset = DicomMetaDictionary.naturalizeDataset(dicomData.dict);
    rtStructDataset._meta = DicomMetaDictionary.namifyDataset(dicomData.meta);
    instance = rtStructDataset;
  } else {
    await checkAndLoadContourData(instance, dataSource);
  }
  const {
    StructureSetROISequence,
    ROIContourSequence,
    RTROIObservationsSequence
  } = instance;

  // Define our structure set entry and add it to the rtstruct module state.
  const structureSet = {
    StructureSetLabel: instance.StructureSetLabel,
    SeriesInstanceUID: instance.SeriesInstanceUID,
    ROIContours: [],
    visible: true
  };
  for (let i = 0; i < ROIContourSequence.length; i++) {
    const ROIContour = ROIContourSequence[i];
    const {
      ContourSequence
    } = ROIContour;
    if (!ContourSequence) {
      continue;
    }
    const isSupported = false;
    const ContourSequenceArray = _toArray(ContourSequence);
    const contourPoints = [];
    for (let c = 0; c < ContourSequenceArray.length; c++) {
      const {
        ContourImageSequence,
        ContourData,
        NumberOfContourPoints,
        ContourGeometricType
      } = ContourSequenceArray[c];
      let isSupported = false;
      const points = [];
      for (let p = 0; p < NumberOfContourPoints * 3; p += 3) {
        points.push({
          x: ContourData[p],
          y: ContourData[p + 1],
          z: ContourData[p + 2]
        });
      }
      switch (ContourGeometricType) {
        case 'CLOSED_PLANAR':
        case 'OPEN_PLANAR':
        case 'POINT':
          isSupported = true;
          break;
        default:
          continue;
      }
      contourPoints.push({
        numberOfPoints: NumberOfContourPoints,
        points,
        type: ContourGeometricType,
        isSupported
      });
    }
    _setROIContourMetadata(structureSet, StructureSetROISequence, RTROIObservationsSequence, ROIContour, contourPoints, isSupported);
  }
  return structureSet;
}
const _getImageId = (imageIdSopInstanceUidPairs, sopInstanceUID) => {
  const imageIdSopInstanceUidPairsEntry = imageIdSopInstanceUidPairs.find(imageIdSopInstanceUidPairsEntry => imageIdSopInstanceUidPairsEntry.sopInstanceUID === sopInstanceUID);
  return imageIdSopInstanceUidPairsEntry ? imageIdSopInstanceUidPairsEntry.imageId : null;
};
function _getImageIdSopInstanceUidPairsForDisplaySet(referencedDisplaySet) {
  return referencedDisplaySet.images.map(image => {
    return {
      imageId: image.imageId,
      sopInstanceUID: image.SOPInstanceUID
    };
  });
}
function _setROIContourMetadata(structureSet, StructureSetROISequence, RTROIObservationsSequence, ROIContour, contourPoints, isSupported) {
  const StructureSetROI = StructureSetROISequence.find(structureSetROI => structureSetROI.ROINumber === ROIContour.ReferencedROINumber);
  const ROIContourData = {
    ROINumber: StructureSetROI.ROINumber,
    ROIName: StructureSetROI.ROIName,
    ROIGenerationAlgorithm: StructureSetROI.ROIGenerationAlgorithm,
    ROIDescription: StructureSetROI.ROIDescription,
    isSupported,
    contourPoints,
    visible: true
  };
  _setROIContourDataColor(ROIContour, ROIContourData);
  if (RTROIObservationsSequence) {
    // If present, add additional RTROIObservations metadata.
    _setROIContourRTROIObservations(ROIContourData, RTROIObservationsSequence, ROIContour.ReferencedROINumber);
  }
  structureSet.ROIContours.push(ROIContourData);
}
function _setROIContourDataColor(ROIContour, ROIContourData) {
  let {
    ROIDisplayColor,
    RecommendedDisplayCIELabValue
  } = ROIContour;
  if (!ROIDisplayColor && RecommendedDisplayCIELabValue) {
    // If ROIDisplayColor is absent, try using the RecommendedDisplayCIELabValue color.
    ROIDisplayColor = dicomlab2RGB(RecommendedDisplayCIELabValue);
  }
  if (ROIDisplayColor) {
    ROIContourData.colorArray = [...ROIDisplayColor];
  }
}
function _setROIContourRTROIObservations(ROIContourData, RTROIObservationsSequence, ROINumber) {
  const RTROIObservations = RTROIObservationsSequence.find(RTROIObservations => RTROIObservations.ReferencedROINumber === ROINumber);
  if (RTROIObservations) {
    // Deep copy so we don't keep the reference to the dcmjs dataset entry.
    const {
      ObservationNumber,
      ROIObservationDescription,
      RTROIInterpretedType,
      ROIInterpreter
    } = RTROIObservations;
    ROIContourData.RTROIObservations = {
      ObservationNumber,
      ROIObservationDescription,
      RTROIInterpretedType,
      ROIInterpreter
    };
  }
}
function _toArray(objOrArray) {
  return Array.isArray(objOrArray) ? objOrArray : [objOrArray];
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/src/getSopClassHandlerModule.js



const sopClassUids = ['1.2.840.10008.5.1.4.1.1.481.3'];
let loadPromises = {};
function _getDisplaySetsFromSeries(instances, servicesManager, extensionManager) {
  const instance = instances[0];
  const {
    StudyInstanceUID,
    SeriesInstanceUID,
    SOPInstanceUID,
    SeriesDescription,
    SeriesNumber,
    SeriesDate,
    SOPClassUID,
    wadoRoot,
    wadoUri,
    wadoUriRoot
  } = instance;
  const displaySet = {
    Modality: 'RTSTRUCT',
    loading: false,
    isReconstructable: false,
    // by default for now since it is a volumetric SEG currently
    displaySetInstanceUID: src.utils.guid(),
    SeriesDescription,
    SeriesNumber,
    SeriesDate,
    SOPInstanceUID,
    SeriesInstanceUID,
    StudyInstanceUID,
    SOPClassHandlerId: SOPClassHandlerId,
    SOPClassUID,
    referencedImages: null,
    referencedSeriesInstanceUID: null,
    referencedDisplaySetInstanceUID: null,
    isDerivedDisplaySet: true,
    isLoaded: false,
    isHydrated: false,
    structureSet: null,
    sopClassUids,
    instance,
    wadoRoot,
    wadoUriRoot,
    wadoUri,
    isOverlayDisplaySet: true
  };
  let referencedSeriesSequence = instance.ReferencedSeriesSequence;
  if (instance.ReferencedFrameOfReferenceSequence && !instance.ReferencedSeriesSequence) {
    instance.ReferencedSeriesSequence = _deriveReferencedSeriesSequenceFromFrameOfReferenceSequence(instance.ReferencedFrameOfReferenceSequence);
    referencedSeriesSequence = instance.ReferencedSeriesSequence;
  }
  if (!referencedSeriesSequence) {
    throw new Error('ReferencedSeriesSequence is missing for the RTSTRUCT');
  }
  const referencedSeries = referencedSeriesSequence[0];
  displaySet.referencedImages = instance.ReferencedSeriesSequence.ReferencedInstanceSequence;
  displaySet.referencedSeriesInstanceUID = referencedSeries.SeriesInstanceUID;
  displaySet.getReferenceDisplaySet = () => {
    const {
      DisplaySetService
    } = servicesManager.services;
    const referencedDisplaySets = DisplaySetService.getDisplaySetsForSeries(displaySet.referencedSeriesInstanceUID);
    if (!referencedDisplaySets || referencedDisplaySets.length === 0) {
      throw new Error('Referenced DisplaySet is missing for the RT');
    }
    const referencedDisplaySet = referencedDisplaySets[0];
    displaySet.referencedDisplaySetInstanceUID = referencedDisplaySet.displaySetInstanceUID;
    return referencedDisplaySet;
  };
  displaySet.load = _ref => {
    let {
      headers
    } = _ref;
    return _load(displaySet, servicesManager, extensionManager, headers);
  };
  return [displaySet];
}
function _load(rtDisplaySet, servicesManager, extensionManager, headers) {
  const {
    SOPInstanceUID
  } = rtDisplaySet;
  const {
    segmentationService
  } = servicesManager.services;
  if ((rtDisplaySet.loading || rtDisplaySet.isLoaded) && loadPromises[SOPInstanceUID] && _segmentationExistsInCache(rtDisplaySet, segmentationService)) {
    return loadPromises[SOPInstanceUID];
  }
  rtDisplaySet.loading = true;

  // We don't want to fire multiple loads, so we'll wait for the first to finish
  // and also return the same promise to any other callers.
  loadPromises[SOPInstanceUID] = new Promise(async (resolve, reject) => {
    if (!rtDisplaySet.structureSet) {
      const structureSet = await loadRTStruct(extensionManager, rtDisplaySet, rtDisplaySet.getReferenceDisplaySet(), headers);
      rtDisplaySet.structureSet = structureSet;
    }
    const suppressEvents = true;
    segmentationService.createSegmentationForRTDisplaySet(rtDisplaySet, null, suppressEvents).then(() => {
      rtDisplaySet.loading = false;
      resolve();
    }).catch(error => {
      rtDisplaySet.loading = false;
      reject(error);
    });
  });
  return loadPromises[SOPInstanceUID];
}
function _deriveReferencedSeriesSequenceFromFrameOfReferenceSequence(ReferencedFrameOfReferenceSequence) {
  const ReferencedSeriesSequence = [];
  ReferencedFrameOfReferenceSequence.forEach(referencedFrameOfReference => {
    const {
      RTReferencedStudySequence
    } = referencedFrameOfReference;
    RTReferencedStudySequence.forEach(rtReferencedStudy => {
      const {
        RTReferencedSeriesSequence
      } = rtReferencedStudy;
      RTReferencedSeriesSequence.forEach(rtReferencedSeries => {
        const ReferencedInstanceSequence = [];
        const {
          ContourImageSequence,
          SeriesInstanceUID
        } = rtReferencedSeries;
        ContourImageSequence.forEach(contourImage => {
          ReferencedInstanceSequence.push({
            ReferencedSOPInstanceUID: contourImage.ReferencedSOPInstanceUID,
            ReferencedSOPClassUID: contourImage.ReferencedSOPClassUID
          });
        });
        const referencedSeries = {
          SeriesInstanceUID,
          ReferencedInstanceSequence
        };
        ReferencedSeriesSequence.push(referencedSeries);
      });
    });
  });
  return ReferencedSeriesSequence;
}
function _segmentationExistsInCache(rtDisplaySet, segmentationService) {
  // Todo: fix this
  return false;
  // This should be abstracted with the CornerstoneCacheService
  const rtContourId = rtDisplaySet.displaySetInstanceUID;
  const contour = segmentationService.getContour(rtContourId);
  return contour !== undefined;
}
function getSopClassHandlerModule(_ref2) {
  let {
    servicesManager,
    extensionManager
  } = _ref2;
  return [{
    name: 'dicom-rt',
    sopClassUids,
    getDisplaySetsFromSeries: instances => {
      return _getDisplaySetsFromSeries(instances, servicesManager, extensionManager);
    }
  }];
}
/* harmony default export */ const src_getSopClassHandlerModule = (getSopClassHandlerModule);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/src/index.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }



const Component = /*#__PURE__*/react.lazy(() => {
  return __webpack_require__.e(/* import() */ 471).then(__webpack_require__.bind(__webpack_require__, 56471));
});
const OHIFCornerstoneRTViewport = props => {
  return /*#__PURE__*/react.createElement(react.Suspense, {
    fallback: /*#__PURE__*/react.createElement("div", null, "Loading...")
  }, /*#__PURE__*/react.createElement(Component, props));
};

/**
 * You can remove any of the following modules if you don't need them.
 */
const extension = {
  /**
   * Only required property. Should be a unique value across all extensions.
   * You ID can be anything you want, but it should be unique.
   */
  id: id,
  /**
   * PanelModule should provide a list of panels that will be available in OHIF
   * for Modes to consume and render. Each panel is defined by a {name,
   * iconName, iconLabel, label, component} object. Example of a panel module
   * is the StudyBrowserPanel that is provided by the default extension in OHIF.
   */
  getViewportModule(_ref) {
    let {
      servicesManager,
      extensionManager,
      commandsManager
    } = _ref;
    const ExtendedOHIFCornerstoneRTViewport = props => {
      return /*#__PURE__*/react.createElement(OHIFCornerstoneRTViewport, _extends({
        servicesManager: servicesManager,
        extensionManager: extensionManager,
        commandsManager: commandsManager
      }, props));
    };
    return [{
      name: 'dicom-rt',
      component: ExtendedOHIFCornerstoneRTViewport
    }];
  },
  /**
   * SopClassHandlerModule should provide a list of sop class handlers that will be
   * available in OHIF for Modes to consume and use to create displaySets from Series.
   * Each sop class handler is defined by a { name, sopClassUids, getDisplaySetsFromSeries}.
   * Examples include the default sop class handler provided by the default extension
   */
  getSopClassHandlerModule: src_getSopClassHandlerModule
};
/* harmony default export */ const cornerstone_dicom_rt_src = (extension);

/***/ })

}]);