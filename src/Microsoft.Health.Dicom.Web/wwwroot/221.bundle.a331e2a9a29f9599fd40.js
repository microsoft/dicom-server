(self["webpackChunk"] = self["webpackChunk"] || []).push([[221,579],{

/***/ 9943:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ cornerstone_dicom_seg_src)
});

;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-cornerstone-dicom-seg"}');
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/id.js

const id = package_namespaceObject.u2;
const SOPClassHandlerName = 'dicom-seg';
const SOPClassHandlerId = `${id}.sopClassHandlerModule.${SOPClassHandlerName}`;

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var esm = __webpack_require__(3743);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/adapters/dist/adapters.es.js
var adapters_es = __webpack_require__(91202);
// EXTERNAL MODULE: ../../../node_modules/dcmjs/build/dcmjs.es.js
var dcmjs_es = __webpack_require__(67540);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/utils/dicomlabToRGB.ts


/**
 * Converts a CIELAB color to an RGB color using the dcmjs library.
 * @param cielab - The CIELAB color to convert.
 * @returns The RGB color as an array of three integers between 0 and 255.
 */
function dicomlabToRGB(cielab) {
  const rgb = dcmjs_es["default"].data.Colors.dicomlab2RGB(cielab).map(x => Math.round(x * 255));
  return rgb;
}

;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/getSopClassHandlerModule.js





const sopClassUids = ['1.2.840.10008.5.1.4.1.1.66.4'];
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
    Modality: 'SEG',
    loading: false,
    isReconstructable: true,
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
    segments: {},
    sopClassUids,
    instance,
    instances: [instance],
    wadoRoot,
    wadoUriRoot,
    wadoUri,
    isOverlayDisplaySet: true
  };
  const referencedSeriesSequence = instance.ReferencedSeriesSequence;
  if (!referencedSeriesSequence) {
    console.error('ReferencedSeriesSequence is missing for the SEG');
    return;
  }
  const referencedSeries = referencedSeriesSequence[0] || referencedSeriesSequence;
  displaySet.referencedImages = instance.ReferencedSeriesSequence.ReferencedInstanceSequence;
  displaySet.referencedSeriesInstanceUID = referencedSeries.SeriesInstanceUID;
  displaySet.getReferenceDisplaySet = () => {
    const {
      displaySetService
    } = servicesManager.services;
    const referencedDisplaySets = displaySetService.getDisplaySetsForSeries(displaySet.referencedSeriesInstanceUID);
    if (!referencedDisplaySets || referencedDisplaySets.length === 0) {
      throw new Error('Referenced DisplaySet is missing for the SEG');
    }
    const referencedDisplaySet = referencedDisplaySets[0];
    displaySet.referencedDisplaySetInstanceUID = referencedDisplaySet.displaySetInstanceUID;

    // Todo: this needs to be able to work with other reference volumes (other than streaming) such as nifti, etc.
    displaySet.referencedVolumeURI = referencedDisplaySet.displaySetInstanceUID;
    const referencedVolumeId = `cornerstoneStreamingImageVolume:${displaySet.referencedVolumeURI}`;
    displaySet.referencedVolumeId = referencedVolumeId;
    return referencedDisplaySet;
  };
  displaySet.load = async _ref => {
    let {
      headers
    } = _ref;
    return await _load(displaySet, servicesManager, extensionManager, headers);
  };
  return [displaySet];
}
function _load(segDisplaySet, servicesManager, extensionManager, headers) {
  const {
    SOPInstanceUID
  } = segDisplaySet;
  const {
    segmentationService
  } = servicesManager.services;
  if ((segDisplaySet.loading || segDisplaySet.isLoaded) && loadPromises[SOPInstanceUID] && _segmentationExists(segDisplaySet, segmentationService)) {
    return loadPromises[SOPInstanceUID];
  }
  segDisplaySet.loading = true;

  // We don't want to fire multiple loads, so we'll wait for the first to finish
  // and also return the same promise to any other callers.
  loadPromises[SOPInstanceUID] = new Promise(async (resolve, reject) => {
    if (!segDisplaySet.segments || Object.keys(segDisplaySet.segments).length === 0) {
      await _loadSegments({
        extensionManager,
        servicesManager,
        segDisplaySet,
        headers
      });
    }
    const suppressEvents = true;
    segmentationService.createSegmentationForSEGDisplaySet(segDisplaySet, null, suppressEvents).then(() => {
      segDisplaySet.loading = false;
      resolve();
    }).catch(error => {
      segDisplaySet.loading = false;
      reject(error);
    });
  });
  return loadPromises[SOPInstanceUID];
}
async function _loadSegments(_ref2) {
  let {
    extensionManager,
    servicesManager,
    segDisplaySet,
    headers
  } = _ref2;
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.common');
  const {
    segmentationService
  } = servicesManager.services;
  const {
    dicomLoaderService
  } = utilityModule.exports;
  const arrayBuffer = await dicomLoaderService.findDicomDataPromise(segDisplaySet, null, headers);
  const cachedReferencedVolume = esm.cache.getVolume(segDisplaySet.referencedVolumeId);
  if (!cachedReferencedVolume) {
    throw new Error('Referenced Volume is missing for the SEG, and stack viewport SEG is not supported yet');
  }
  const {
    imageIds
  } = cachedReferencedVolume;

  // Todo: what should be defaults here
  const tolerance = 0.001;
  const skipOverlapping = true;
  esm.eventTarget.addEventListener(adapters_es/* Enums */.Y.Events.SEGMENTATION_LOAD_PROGRESS, evt => {
    const {
      percentComplete
    } = evt.detail;
    segmentationService._broadcastEvent(segmentationService.EVENTS.SEGMENT_LOADING_COMPLETE, {
      percentComplete
    });
  });
  const results = await adapters_es.adaptersSEG.Cornerstone3D.Segmentation.generateToolState(imageIds, arrayBuffer, esm.metaData, {
    skipOverlapping,
    tolerance,
    eventTarget: esm.eventTarget,
    triggerEvent: esm.triggerEvent
  });
  results.segMetadata.data.forEach((data, i) => {
    if (i > 0) {
      data.rgba = dicomlabToRGB(data.RecommendedDisplayCIELabValue);
    }
  });
  Object.assign(segDisplaySet, results);
}
function _segmentationExists(segDisplaySet, segmentationService) {
  // This should be abstracted with the CornerstoneCacheService
  return segmentationService.getSegmentation(segDisplaySet.displaySetInstanceUID);
}
function getSopClassHandlerModule(_ref3) {
  let {
    servicesManager,
    extensionManager
  } = _ref3;
  const getDisplaySetsFromSeries = instances => {
    return _getDisplaySetsFromSeries(instances, servicesManager, extensionManager);
  };
  return [{
    name: 'dicom-seg',
    sopClassUids,
    getDisplaySetsFromSeries
  }];
}
/* harmony default export */ const src_getSopClassHandlerModule = (getSopClassHandlerModule);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/getHangingProtocolModule.ts
const segProtocol = {
  id: '@ohif/seg',
  // Don't store this hanging protocol as it applies to the currently active
  // display set by default
  // cacheId: null,
  name: 'Segmentations',
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
      id: 'segDisplaySetId',
      matchedDisplaySetsIndex: -1
    }]
  },
  displaySetSelectors: {
    segDisplaySetId: {
      seriesMatchingRules: [{
        attribute: 'Modality',
        constraint: {
          equals: 'SEG'
        }
      }]
    }
  },
  stages: [{
    name: 'Segmentations',
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
        id: 'segDisplaySetId'
      }]
    }]
  }]
};
function getHangingProtocolModule() {
  return [{
    name: segProtocol.id,
    protocol: segProtocol
  }];
}
/* harmony default export */ const src_getHangingProtocolModule = (getHangingProtocolModule);

// EXTERNAL MODULE: ./state/index.js + 1 modules
var state = __webpack_require__(62657);
// EXTERNAL MODULE: ../../../extensions/default/src/index.ts + 76 modules
var default_src = __webpack_require__(56342);
// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/panels/callInputDialog.tsx


function callInputDialog(uiDialogService, label, callback) {
  const dialogId = 'enter-segment-label';
  const onSubmitHandler = _ref => {
    let {
      action,
      value
    } = _ref;
    switch (action.id) {
      case 'save':
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
        title: 'Segment',
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
          text: 'Confirm',
          type: ui_src/* ButtonEnums.type */.LZ.dt.primary
        }],
        onSubmit: onSubmitHandler,
        body: _ref2 => {
          let {
            value,
            setValue
          } = _ref2;
          return /*#__PURE__*/react.createElement(ui_src/* Input */.II, {
            label: "Enter the segment label",
            labelClassName: "text-white text-[14px] leading-[1.2]",
            autoFocus: true,
            className: "border-primary-main bg-black",
            type: "text",
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
/* harmony default export */ const panels_callInputDialog = (callInputDialog);
// EXTERNAL MODULE: ../../../node_modules/react-color/es/index.js + 219 modules
var es = __webpack_require__(22831);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/panels/colorPickerDialog.css
// extracted by mini-css-extract-plugin

;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/panels/colorPickerDialog.tsx




function callColorPickerDialog(uiDialogService, rgbaColor, callback) {
  const dialogId = 'pick-color';
  const onSubmitHandler = _ref => {
    let {
      action,
      value
    } = _ref;
    switch (action.id) {
      case 'save':
        callback(value.rgbaColor, action.id);
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
        title: 'Segment Color',
        value: {
          rgbaColor
        },
        noCloseButton: true,
        onClose: () => uiDialogService.dismiss({
          id: dialogId
        }),
        actions: [{
          id: 'cancel',
          text: 'Cancel',
          type: 'primary'
        }, {
          id: 'save',
          text: 'Save',
          type: 'secondary'
        }],
        onSubmit: onSubmitHandler,
        body: _ref2 => {
          let {
            value,
            setValue
          } = _ref2;
          const handleChange = color => {
            setValue({
              rgbaColor: color.rgb
            });
          };
          return /*#__PURE__*/react.createElement(es/* ChromePicker */.AI, {
            color: value.rgbaColor,
            onChange: handleChange,
            presetColors: [],
            width: 300
          });
        }
      }
    });
  }
}
/* harmony default export */ const colorPickerDialog = (callColorPickerDialog);
// EXTERNAL MODULE: ../../../node_modules/react-i18next/dist/es/index.js + 15 modules
var dist_es = __webpack_require__(69190);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/panels/PanelSegmentation.tsx







function PanelSegmentation(_ref) {
  let {
    servicesManager,
    commandsManager,
    extensionManager,
    configuration
  } = _ref;
  const {
    segmentationService,
    viewportGridService,
    uiDialogService
  } = servicesManager.services;
  const {
    t
  } = (0,dist_es/* useTranslation */.$G)('PanelSegmentation');
  const [selectedSegmentationId, setSelectedSegmentationId] = (0,react.useState)(null);
  const [segmentationConfiguration, setSegmentationConfiguration] = (0,react.useState)(segmentationService.getConfiguration());
  const [segmentations, setSegmentations] = (0,react.useState)(() => segmentationService.getSegmentations());
  (0,react.useEffect)(() => {
    // ~~ Subscription
    const added = segmentationService.EVENTS.SEGMENTATION_ADDED;
    const updated = segmentationService.EVENTS.SEGMENTATION_UPDATED;
    const removed = segmentationService.EVENTS.SEGMENTATION_REMOVED;
    const subscriptions = [];
    [added, updated, removed].forEach(evt => {
      const {
        unsubscribe
      } = segmentationService.subscribe(evt, () => {
        const segmentations = segmentationService.getSegmentations();
        setSegmentations(segmentations);
        setSegmentationConfiguration(segmentationService.getConfiguration());
      });
      subscriptions.push(unsubscribe);
    });
    return () => {
      subscriptions.forEach(unsub => {
        unsub();
      });
    };
  }, []);
  const getToolGroupIds = segmentationId => {
    const toolGroupIds = segmentationService.getToolGroupIdsWithSegmentation(segmentationId);
    return toolGroupIds;
  };
  const onSegmentationAdd = async () => {
    commandsManager.runCommand('createEmptySegmentationForViewport');
  };
  const onSegmentationClick = segmentationId => {
    segmentationService.setActiveSegmentationForToolGroup(segmentationId);
  };
  const onSegmentationDelete = segmentationId => {
    segmentationService.remove(segmentationId);
  };
  const onSegmentAdd = segmentationId => {
    segmentationService.addSegment(segmentationId);
  };
  const onSegmentClick = (segmentationId, segmentIndex) => {
    segmentationService.setActiveSegment(segmentationId, segmentIndex);
    const toolGroupIds = getToolGroupIds(segmentationId);
    toolGroupIds.forEach(toolGroupId => {
      // const toolGroupId =
      segmentationService.setActiveSegmentationForToolGroup(segmentationId, toolGroupId);
      segmentationService.jumpToSegmentCenter(segmentationId, segmentIndex, toolGroupId);
    });
  };
  const onSegmentEdit = (segmentationId, segmentIndex) => {
    const segmentation = segmentationService.getSegmentation(segmentationId);
    const segment = segmentation.segments[segmentIndex];
    const {
      label
    } = segment;
    panels_callInputDialog(uiDialogService, label, (label, actionId) => {
      if (label === '') {
        return;
      }
      segmentationService.setSegmentLabel(segmentationId, segmentIndex, label);
    });
  };
  const onSegmentationEdit = segmentationId => {
    const segmentation = segmentationService.getSegmentation(segmentationId);
    const {
      label
    } = segmentation;
    panels_callInputDialog(uiDialogService, label, (label, actionId) => {
      if (label === '') {
        return;
      }
      segmentationService.addOrUpdateSegmentation({
        id: segmentationId,
        label
      }, false,
      // suppress event
      true // notYetUpdatedAtSource
      );
    });
  };

  const onSegmentColorClick = (segmentationId, segmentIndex) => {
    const segmentation = segmentationService.getSegmentation(segmentationId);
    const segment = segmentation.segments[segmentIndex];
    const {
      color,
      opacity
    } = segment;
    const rgbaColor = {
      r: color[0],
      g: color[1],
      b: color[2],
      a: opacity / 255.0
    };
    colorPickerDialog(uiDialogService, rgbaColor, (newRgbaColor, actionId) => {
      if (actionId === 'cancel') {
        return;
      }
      segmentationService.setSegmentRGBAColor(segmentationId, segmentIndex, [newRgbaColor.r, newRgbaColor.g, newRgbaColor.b, newRgbaColor.a * 255.0]);
    });
  };
  const onSegmentDelete = (segmentationId, segmentIndex) => {
    segmentationService.removeSegment(segmentationId, segmentIndex);
  };
  const onToggleSegmentVisibility = (segmentationId, segmentIndex) => {
    const segmentation = segmentationService.getSegmentation(segmentationId);
    const segmentInfo = segmentation.segments[segmentIndex];
    const isVisible = !segmentInfo.isVisible;
    const toolGroupIds = getToolGroupIds(segmentationId);

    // Todo: right now we apply the visibility to all tool groups
    toolGroupIds.forEach(toolGroupId => {
      segmentationService.setSegmentVisibility(segmentationId, segmentIndex, isVisible, toolGroupId);
    });
  };
  const onToggleSegmentLock = (segmentationId, segmentIndex) => {
    segmentationService.toggleSegmentLocked(segmentationId, segmentIndex);
  };
  const onToggleSegmentationVisibility = segmentationId => {
    segmentationService.toggleSegmentationVisibility(segmentationId);
  };
  const _setSegmentationConfiguration = (0,react.useCallback)((segmentationId, key, value) => {
    segmentationService.setConfiguration({
      segmentationId,
      [key]: value
    });
  }, [segmentationService]);
  const onSegmentationDownload = segmentationId => {
    commandsManager.runCommand('downloadSegmentation', {
      segmentationId
    });
  };
  const storeSegmentation = async segmentationId => {
    const datasources = extensionManager.getActiveDataSource();
    const displaySetInstanceUIDs = await (0,default_src.createReportAsync)({
      servicesManager,
      getReport: () => commandsManager.runCommand('storeSegmentation', {
        segmentationId,
        dataSource: datasources[0]
      }),
      reportType: 'Segmentation'
    });

    // Show the exported report in the active viewport as read only (similar to SR)
    if (displaySetInstanceUIDs) {
      // clear the segmentation that we exported, similar to the storeMeasurement
      // where we remove the measurements and prompt again the user if they would like
      // to re-read the measurements in a SR read only viewport
      segmentationService.remove(segmentationId);
      viewportGridService.setDisplaySetsForViewport({
        viewportId: viewportGridService.getActiveViewportId(),
        displaySetInstanceUIDs
      });
    }
  };
  const onSegmentationDownloadRTSS = segmentationId => {
    commandsManager.runCommand('downloadRTSS', {
      segmentationId
    });
  };
  return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("div", {
    className: "ohif-scrollbar flex min-h-0 flex-auto select-none flex-col justify-between overflow-auto"
  }, /*#__PURE__*/react.createElement(ui_src/* SegmentationGroupTable */.cX, {
    title: t('Segmentations'),
    segmentations: segmentations,
    disableEditing: configuration.disableEditing,
    activeSegmentationId: selectedSegmentationId || '',
    onSegmentationAdd: onSegmentationAdd,
    onSegmentationClick: onSegmentationClick,
    onSegmentationDelete: onSegmentationDelete,
    onSegmentationDownload: onSegmentationDownload,
    onSegmentationDownloadRTSS: onSegmentationDownloadRTSS,
    storeSegmentation: storeSegmentation,
    onSegmentationEdit: onSegmentationEdit,
    onSegmentClick: onSegmentClick,
    onSegmentEdit: onSegmentEdit,
    onSegmentAdd: onSegmentAdd,
    onSegmentColorClick: onSegmentColorClick,
    onSegmentDelete: onSegmentDelete,
    onToggleSegmentVisibility: onToggleSegmentVisibility,
    onToggleSegmentLock: onToggleSegmentLock,
    onToggleSegmentationVisibility: onToggleSegmentationVisibility,
    showDeleteSegment: true,
    segmentationConfig: {
      initialConfig: segmentationConfiguration
    },
    setRenderOutline: value => _setSegmentationConfiguration(selectedSegmentationId, 'renderOutline', value),
    setOutlineOpacityActive: value => _setSegmentationConfiguration(selectedSegmentationId, 'outlineOpacity', value),
    setRenderFill: value => _setSegmentationConfiguration(selectedSegmentationId, 'renderFill', value),
    setRenderInactiveSegmentations: value => _setSegmentationConfiguration(selectedSegmentationId, 'renderInactiveSegmentations', value),
    setOutlineWidthActive: value => _setSegmentationConfiguration(selectedSegmentationId, 'outlineWidthActive', value),
    setFillAlpha: value => _setSegmentationConfiguration(selectedSegmentationId, 'fillAlpha', value),
    setFillAlphaInactive: value => _setSegmentationConfiguration(selectedSegmentationId, 'fillAlphaInactive', value)
  })));
}
PanelSegmentation.propTypes = {
  commandsManager: prop_types_default().shape({
    runCommand: (prop_types_default()).func.isRequired
  }),
  servicesManager: prop_types_default().shape({
    services: prop_types_default().shape({
      segmentationService: prop_types_default().shape({
        getSegmentation: (prop_types_default()).func.isRequired,
        getSegmentations: (prop_types_default()).func.isRequired,
        toggleSegmentationVisibility: (prop_types_default()).func.isRequired,
        subscribe: (prop_types_default()).func.isRequired,
        EVENTS: (prop_types_default()).object.isRequired
      }).isRequired
    }).isRequired
  }).isRequired
};
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/tools/dist/esm/index.js + 348 modules
var dist_esm = __webpack_require__(14957);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/panels/SegmentationToolbox.tsx



const {
  segmentation: segmentationUtils
} = dist_esm.utilities;
const TOOL_TYPES = {
  CIRCULAR_BRUSH: 'CircularBrush',
  SPHERE_BRUSH: 'SphereBrush',
  CIRCULAR_ERASER: 'CircularEraser',
  SPHERE_ERASER: 'SphereEraser',
  CIRCLE_SHAPE: 'CircleScissor',
  RECTANGLE_SHAPE: 'RectangleScissor',
  SPHERE_SHAPE: 'SphereScissor',
  THRESHOLD_CIRCULAR_BRUSH: 'ThresholdCircularBrush',
  THRESHOLD_SPHERE_BRUSH: 'ThresholdSphereBrush'
};
const ACTIONS = {
  SET_TOOL_CONFIG: 'SET_TOOL_CONFIG',
  SET_ACTIVE_TOOL: 'SET_ACTIVE_TOOL'
};
const initialState = {
  Brush: {
    brushSize: 15,
    mode: 'CircularBrush' // Can be 'CircularBrush' or 'SphereBrush'
  },

  Eraser: {
    brushSize: 15,
    mode: 'CircularEraser' // Can be 'CircularEraser' or 'SphereEraser'
  },

  Shapes: {
    brushSize: 15,
    mode: 'CircleScissor' // E.g., 'CircleScissor', 'RectangleScissor', or 'SphereScissor'
  },

  ThresholdBrush: {
    brushSize: 15,
    thresholdRange: [-500, 500]
  },
  activeTool: null
};
function toolboxReducer(state, action) {
  switch (action.type) {
    case ACTIONS.SET_TOOL_CONFIG:
      const {
        tool,
        config
      } = action.payload;
      return {
        ...state,
        [tool]: {
          ...state[tool],
          ...config
        }
      };
    case ACTIONS.SET_ACTIVE_TOOL:
      return {
        ...state,
        activeTool: action.payload
      };
    default:
      return state;
  }
}
function SegmentationToolbox(_ref) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const {
    toolbarService,
    segmentationService,
    toolGroupService
  } = servicesManager.services;
  const [viewportGrid] = (0,ui_src/* useViewportGrid */.O_)();
  const {
    viewports,
    activeViewportId
  } = viewportGrid;
  const [toolsEnabled, setToolsEnabled] = (0,react.useState)(false);
  const [state, dispatch] = (0,react.useReducer)(toolboxReducer, initialState);
  const updateActiveTool = (0,react.useCallback)(() => {
    if (!viewports?.size || activeViewportId === undefined) {
      return;
    }
    const viewport = viewports.get(activeViewportId);
    if (!viewport) {
      return;
    }
    dispatch({
      type: ACTIONS.SET_ACTIVE_TOOL,
      payload: toolGroupService.getActiveToolForViewport(viewport.viewportId)
    });
  }, [activeViewportId, viewports, toolGroupService, dispatch]);
  const setToolActive = (0,react.useCallback)(toolName => {
    toolbarService.recordInteraction({
      interactionType: 'tool',
      commands: [{
        commandName: 'setToolActive',
        commandOptions: {
          toolName
        }
      }]
    });
    dispatch({
      type: ACTIONS.SET_ACTIVE_TOOL,
      payload: toolName
    });
  }, [toolbarService, dispatch]);

  /**
   * sets the tools enabled IF there are segmentations
   */
  (0,react.useEffect)(() => {
    const events = [segmentationService.EVENTS.SEGMENTATION_ADDED, segmentationService.EVENTS.SEGMENTATION_UPDATED, segmentationService.EVENTS.SEGMENTATION_REMOVED];
    const unsubscriptions = [];
    events.forEach(event => {
      const {
        unsubscribe
      } = segmentationService.subscribe(event, () => {
        const segmentations = segmentationService.getSegmentations();
        const activeSegmentation = segmentations?.find(seg => seg.isActive);
        setToolsEnabled(activeSegmentation?.segmentCount > 0);
      });
      unsubscriptions.push(unsubscribe);
    });
    updateActiveTool();
    return () => {
      unsubscriptions.forEach(unsubscribe => unsubscribe());
    };
  }, [activeViewportId, viewports, segmentationService, updateActiveTool]);

  /**
   * Update the active tool when the toolbar state changes
   */
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = toolbarService.subscribe(toolbarService.EVENTS.TOOL_BAR_STATE_MODIFIED, () => {
      updateActiveTool();
    });
    return () => {
      unsubscribe();
    };
  }, [toolbarService, updateActiveTool]);
  (0,react.useEffect)(() => {
    // if the active tool is not a brush tool then do nothing
    if (!Object.values(TOOL_TYPES).includes(state.activeTool)) {
      return;
    }

    // if the tool is Segmentation and it is enabled then do nothing
    if (toolsEnabled) {
      return;
    }

    // if the tool is Segmentation and it is disabled, then switch
    // back to the window level tool to not confuse the user when no
    // segmentation is active or when there is no segment in the segmentation
    setToolActive('WindowLevel');
  }, [toolsEnabled, state.activeTool, setToolActive]);
  const updateBrushSize = (0,react.useCallback)((toolName, brushSize) => {
    toolGroupService.getToolGroupIds()?.forEach(toolGroupId => {
      segmentationUtils.setBrushSizeForToolGroup(toolGroupId, brushSize, toolName);
    });
  }, [toolGroupService]);
  const onBrushSizeChange = (0,react.useCallback)((valueAsStringOrNumber, toolCategory) => {
    const value = Number(valueAsStringOrNumber);
    _getToolNamesFromCategory(toolCategory).forEach(toolName => {
      updateBrushSize(toolName, value);
    });
    dispatch({
      type: ACTIONS.SET_TOOL_CONFIG,
      payload: {
        tool: toolCategory,
        config: {
          brushSize: value
        }
      }
    });
  }, [toolGroupService, dispatch]);
  const handleRangeChange = (0,react.useCallback)(newRange => {
    if (newRange[0] === state.ThresholdBrush.thresholdRange[0] && newRange[1] === state.ThresholdBrush.thresholdRange[1]) {
      return;
    }
    const toolNames = _getToolNamesFromCategory('ThresholdBrush');
    toolNames.forEach(toolName => {
      toolGroupService.getToolGroupIds()?.forEach(toolGroupId => {
        const toolGroup = toolGroupService.getToolGroup(toolGroupId);
        toolGroup.setToolConfiguration(toolName, {
          strategySpecificConfiguration: {
            THRESHOLD_INSIDE_CIRCLE: {
              threshold: newRange
            }
          }
        });
      });
    });
    dispatch({
      type: ACTIONS.SET_TOOL_CONFIG,
      payload: {
        tool: 'ThresholdBrush',
        config: {
          thresholdRange: newRange
        }
      }
    });
  }, [toolGroupService, dispatch, state.ThresholdBrush.thresholdRange]);
  return /*#__PURE__*/react.createElement(ui_src/* AdvancedToolbox */.bY, {
    title: "Segmentation Tools",
    items: [{
      name: 'Brush',
      icon: 'icon-tool-brush',
      disabled: !toolsEnabled,
      active: state.activeTool === TOOL_TYPES.CIRCULAR_BRUSH || state.activeTool === TOOL_TYPES.SPHERE_BRUSH,
      onClick: () => setToolActive(TOOL_TYPES.CIRCULAR_BRUSH),
      options: [{
        name: 'Radius (mm)',
        id: 'brush-radius',
        type: 'range',
        min: 0.5,
        max: 99.5,
        value: state.Brush.brushSize,
        step: 0.5,
        onChange: value => onBrushSizeChange(value, 'Brush')
      }, {
        name: 'Mode',
        type: 'radio',
        id: 'brush-mode',
        value: state.Brush.mode,
        values: [{
          value: TOOL_TYPES.CIRCULAR_BRUSH,
          label: 'Circle'
        }, {
          value: TOOL_TYPES.SPHERE_BRUSH,
          label: 'Sphere'
        }],
        onChange: value => setToolActive(value)
      }]
    }, {
      name: 'Eraser',
      icon: 'icon-tool-eraser',
      disabled: !toolsEnabled,
      active: state.activeTool === TOOL_TYPES.CIRCULAR_ERASER || state.activeTool === TOOL_TYPES.SPHERE_ERASER,
      onClick: () => setToolActive(TOOL_TYPES.CIRCULAR_ERASER),
      options: [{
        name: 'Radius (mm)',
        type: 'range',
        id: 'eraser-radius',
        min: 0.5,
        max: 99.5,
        value: state.Eraser.brushSize,
        step: 0.5,
        onChange: value => onBrushSizeChange(value, 'Eraser')
      }, {
        name: 'Mode',
        type: 'radio',
        id: 'eraser-mode',
        value: state.Eraser.mode,
        values: [{
          value: TOOL_TYPES.CIRCULAR_ERASER,
          label: 'Circle'
        }, {
          value: TOOL_TYPES.SPHERE_ERASER,
          label: 'Sphere'
        }],
        onChange: value => setToolActive(value)
      }]
    }, {
      name: 'Shapes',
      icon: 'icon-tool-shape',
      disabled: !toolsEnabled,
      active: state.activeTool === TOOL_TYPES.CIRCLE_SHAPE || state.activeTool === TOOL_TYPES.RECTANGLE_SHAPE || state.activeTool === TOOL_TYPES.SPHERE_SHAPE,
      onClick: () => setToolActive(TOOL_TYPES.CIRCLE_SHAPE),
      options: [{
        name: 'Mode',
        type: 'radio',
        value: state.Shapes.mode,
        id: 'shape-mode',
        values: [{
          value: TOOL_TYPES.CIRCLE_SHAPE,
          label: 'Circle'
        }, {
          value: TOOL_TYPES.RECTANGLE_SHAPE,
          label: 'Rectangle'
        }, {
          value: TOOL_TYPES.SPHERE_SHAPE,
          label: 'Sphere'
        }],
        onChange: value => setToolActive(value)
      }]
    }, {
      name: 'Threshold Tool',
      icon: 'icon-tool-threshold',
      disabled: !toolsEnabled,
      active: state.activeTool === TOOL_TYPES.THRESHOLD_CIRCULAR_BRUSH || state.activeTool === TOOL_TYPES.THRESHOLD_SPHERE_BRUSH,
      onClick: () => setToolActive(TOOL_TYPES.THRESHOLD_CIRCULAR_BRUSH),
      options: [{
        name: 'Radius (mm)',
        id: 'threshold-radius',
        type: 'range',
        min: 0.5,
        max: 99.5,
        value: state.ThresholdBrush.brushSize,
        step: 0.5,
        onChange: value => onBrushSizeChange(value, 'ThresholdBrush')
      }, {
        name: 'Mode',
        type: 'radio',
        id: 'threshold-mode',
        value: state.activeTool,
        values: [{
          value: TOOL_TYPES.THRESHOLD_CIRCULAR_BRUSH,
          label: 'Circle'
        }, {
          value: TOOL_TYPES.THRESHOLD_SPHERE_BRUSH,
          label: 'Sphere'
        }],
        onChange: value => setToolActive(value)
      }, {
        type: 'custom',
        id: 'segmentation-threshold-range',
        children: () => {
          return /*#__PURE__*/react.createElement("div", null, /*#__PURE__*/react.createElement("div", {
            className: "bg-secondary-light h-[1px]"
          }), /*#__PURE__*/react.createElement("div", {
            className: "mt-1 text-[13px] text-white"
          }, "Threshold"), /*#__PURE__*/react.createElement(ui_src/* InputDoubleRange */.R0, {
            values: state.ThresholdBrush.thresholdRange,
            onChange: handleRangeChange,
            minValue: -1000,
            maxValue: 1000,
            step: 1,
            showLabel: true,
            allowNumberEdit: true,
            showAdjustmentArrows: false
          }));
        }
      }]
    }]
  });
}
function _getToolNamesFromCategory(category) {
  let toolNames = [];
  switch (category) {
    case 'Brush':
      toolNames = ['CircularBrush', 'SphereBrush'];
      break;
    case 'Eraser':
      toolNames = ['CircularEraser', 'SphereEraser'];
      break;
    case 'ThresholdBrush':
      toolNames = ['ThresholdCircularBrush', 'ThresholdSphereBrush'];
      break;
    default:
      break;
  }
  return toolNames;
}
/* harmony default export */ const panels_SegmentationToolbox = (SegmentationToolbox);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/getPanelModule.tsx




const getPanelModule = _ref => {
  let {
    commandsManager,
    servicesManager,
    extensionManager,
    configuration
  } = _ref;
  const {
    customizationService
  } = servicesManager.services;
  const wrappedPanelSegmentation = configuration => {
    const [appConfig] = (0,state/* useAppConfig */.M)();
    const disableEditingForMode = customizationService.get('segmentation.disableEditing');
    return /*#__PURE__*/react.createElement(PanelSegmentation, {
      commandsManager: commandsManager,
      servicesManager: servicesManager,
      extensionManager: extensionManager,
      configuration: {
        ...configuration,
        disableEditing: appConfig.disableEditing || disableEditingForMode?.value
      }
    });
  };
  const wrappedPanelSegmentationWithTools = configuration => {
    const [appConfig] = (0,state/* useAppConfig */.M)();
    return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement(panels_SegmentationToolbox, {
      commandsManager: commandsManager,
      servicesManager: servicesManager,
      extensionManager: extensionManager,
      configuration: {
        ...configuration
      }
    }), /*#__PURE__*/react.createElement(PanelSegmentation, {
      commandsManager: commandsManager,
      servicesManager: servicesManager,
      extensionManager: extensionManager,
      configuration: {
        ...configuration
      }
    }));
  };
  return [{
    name: 'panelSegmentation',
    iconName: 'tab-segmentation',
    iconLabel: 'Segmentation',
    label: 'Segmentation',
    component: wrappedPanelSegmentation
  }, {
    name: 'panelSegmentationWithTools',
    iconName: 'tab-segmentation',
    iconLabel: 'Segmentation',
    label: 'Segmentation',
    component: wrappedPanelSegmentationWithTools
  }];
};
/* harmony default export */ const src_getPanelModule = (getPanelModule);
// EXTERNAL MODULE: ../../../node_modules/@kitware/vtk.js/Filters/General/ImageMarchingSquares.js + 2 modules
var ImageMarchingSquares = __webpack_require__(49399);
// EXTERNAL MODULE: ../../../node_modules/@kitware/vtk.js/Common/Core/DataArray.js
var DataArray = __webpack_require__(54131);
// EXTERNAL MODULE: ../../../node_modules/@kitware/vtk.js/Common/DataModel/ImageData.js + 2 modules
var ImageData = __webpack_require__(96372);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/utils/hydrationUtils.ts


/**
 * Updates the viewports in preparation for rendering segmentations.
 * Evaluates each viewport to determine which need modifications,
 * then for those viewports, changes them to a volume type and ensures
 * they are ready for segmentation rendering.
 *
 * @param {Object} params - Parameters for the function.
 * @param params.viewportId - ID of the viewport to be updated.
 * @param params.loadFn - Function to load the segmentation data.
 * @param params.servicesManager - The services manager.
 * @param params.referencedDisplaySetInstanceUID - Optional UID for the referenced display set instance.
 *
 * @returns Returns true upon successful update of viewports for segmentation rendering.
 */
async function updateViewportsForSegmentationRendering(_ref) {
  let {
    viewportId,
    loadFn,
    servicesManager,
    referencedDisplaySetInstanceUID
  } = _ref;
  const {
    cornerstoneViewportService,
    segmentationService,
    viewportGridService
  } = servicesManager.services;
  const viewport = getTargetViewport({
    viewportId,
    viewportGridService
  });
  const targetViewportId = viewport.viewportOptions.viewportId;
  referencedDisplaySetInstanceUID = referencedDisplaySetInstanceUID || viewport?.displaySetInstanceUIDs[0];
  const updatedViewports = getUpdatedViewportsForSegmentation({
    servicesManager,
    viewportId,
    referencedDisplaySetInstanceUID
  });

  // create Segmentation callback which needs to be waited until
  // the volume is created (if coming from stack)
  const createSegmentationForVolume = async () => {
    const segmentationId = await loadFn();
    segmentationService.hydrateSegmentation(segmentationId);
  };

  // the reference volume that is used to draw the segmentation. so check if the
  // volume exists in the cache (the target Viewport is already a volume viewport)
  const volumeExists = Array.from(esm.cache._volumeCache.keys()).some(volumeId => volumeId.includes(referencedDisplaySetInstanceUID));
  updatedViewports.forEach(async viewport => {
    viewport.viewportOptions = {
      ...viewport.viewportOptions,
      viewportType: 'volume',
      needsRerendering: true
    };
    const viewportId = viewport.viewportId;
    const csViewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
    const prevCamera = csViewport.getCamera();

    // only run the createSegmentationForVolume for the targetViewportId
    // since the rest will get handled by cornerstoneViewportService
    if (volumeExists && viewportId === targetViewportId) {
      await createSegmentationForVolume();
      return;
    }
    const createNewSegmentationWhenVolumeMounts = async evt => {
      const isTheActiveViewportVolumeMounted = evt.detail.volumeActors?.find(ac => ac.uid.includes(referencedDisplaySetInstanceUID));

      // Note: make sure to re-grab the viewport since it might have changed
      // during the time it took for the volume to be mounted, for instance
      // the stack viewport has been changed to a volume viewport
      const volumeViewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
      volumeViewport.setCamera(prevCamera);
      volumeViewport.element.removeEventListener(esm.Enums.Events.VOLUME_VIEWPORT_NEW_VOLUME, createNewSegmentationWhenVolumeMounts);
      if (!isTheActiveViewportVolumeMounted) {
        // it means it is one of those other updated viewports so just update the camera
        return;
      }
      if (viewportId === targetViewportId) {
        await createSegmentationForVolume();
      }
    };
    csViewport.element.addEventListener(esm.Enums.Events.VOLUME_VIEWPORT_NEW_VOLUME, createNewSegmentationWhenVolumeMounts);
  });

  // Set the displaySets for the viewports that require to be updated
  viewportGridService.setDisplaySetsForViewports(updatedViewports);
  return true;
}
const getTargetViewport = _ref2 => {
  let {
    viewportId,
    viewportGridService
  } = _ref2;
  const {
    viewports,
    activeViewportId
  } = viewportGridService.getState();
  const targetViewportId = viewportId || activeViewportId;
  const viewport = viewports.get(targetViewportId);
  return viewport;
};

/**
 * Retrieves a list of viewports that require updates in preparation for segmentation rendering.
 * This function evaluates viewports based on their compatibility with the provided segmentation's
 * frame of reference UID and appends them to the updated list if they should render the segmentation.
 *
 * @param {Object} params - Parameters for the function.
 * @param params.viewportId - the ID of the viewport to be updated.
 * @param params.servicesManager - The services manager
 * @param params.referencedDisplaySetInstanceUID - Optional UID for the referenced display set instance.
 *
 * @returns {Array} Returns an array of viewports that require updates for segmentation rendering.
 */
function getUpdatedViewportsForSegmentation(_ref3) {
  let {
    viewportId,
    servicesManager,
    referencedDisplaySetInstanceUID
  } = _ref3;
  const {
    hangingProtocolService,
    displaySetService,
    segmentationService,
    viewportGridService
  } = servicesManager.services;
  const {
    viewports
  } = viewportGridService.getState();
  const viewport = getTargetViewport({
    viewportId,
    viewportGridService
  });
  const targetViewportId = viewport.viewportOptions.viewportId;
  const displaySetInstanceUIDs = viewports.get(targetViewportId).displaySetInstanceUIDs;
  const referenceDisplaySetInstanceUID = referencedDisplaySetInstanceUID || displaySetInstanceUIDs[0];
  const referencedDisplaySet = displaySetService.getDisplaySetByUID(referenceDisplaySetInstanceUID);
  const segmentationFrameOfReferenceUID = referencedDisplaySet.instances[0].FrameOfReferenceUID;
  const updatedViewports = hangingProtocolService.getViewportsRequireUpdate(targetViewportId, referenceDisplaySetInstanceUID);
  viewports.forEach((viewport, viewportId) => {
    if (targetViewportId === viewportId || updatedViewports.find(v => v.viewportId === viewportId)) {
      return;
    }
    const shouldDisplaySeg = segmentationService.shouldRenderSegmentation(viewport.displaySetInstanceUIDs, segmentationFrameOfReferenceUID);
    if (shouldDisplaySeg) {
      updatedViewports.push({
        viewportId,
        displaySetInstanceUIDs: viewport.displaySetInstanceUIDs,
        viewportOptions: {
          viewportType: 'volume',
          needsRerendering: true
        }
      });
    }
  });
  return updatedViewports;
}

;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/commandsModule.ts










const {
  datasetToBlob
} = dcmjs_es["default"].data;
const {
  Cornerstone3D: {
    Segmentation: {
      generateLabelMaps2DFrom3D,
      generateSegmentation
    }
  }
} = adapters_es.adaptersSEG;
const {
  Cornerstone3D: {
    RTSS: {
      generateRTSSFromSegmentations
    }
  }
} = adapters_es.adaptersRT;
const {
  downloadDICOMData
} = adapters_es.helpers;
const commandsModule = _ref => {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const {
    uiNotificationService,
    segmentationService,
    uiDialogService,
    displaySetService,
    viewportGridService
  } = servicesManager.services;
  const actions = {
    /**
     * Retrieves a list of viewports that require updates in preparation for segmentation rendering.
     * This function evaluates viewports based on their compatibility with the provided segmentation's
     * frame of reference UID and appends them to the updated list if they should render the segmentation.
     *
     * @param {Object} params - Parameters for the function.
     * @param params.viewportId - the ID of the viewport to be updated.
     * @param params.servicesManager - The services manager
     * @param params.referencedDisplaySetInstanceUID - Optional UID for the referenced display set instance.
     *
     * @returns {Array} Returns an array of viewports that require updates for segmentation rendering.
     */
    getUpdatedViewportsForSegmentation: getUpdatedViewportsForSegmentation,
    /**
     * Creates an empty segmentation for a specified viewport.
     * It first checks if the display set associated with the viewport is reconstructable.
     * If not, it raises a notification error. Otherwise, it creates a new segmentation
     * for the display set after handling the necessary steps for making the viewport
     * a volume viewport first
     *
     * @param {Object} params - Parameters for the function.
     * @param params.viewportId - the target viewport ID.
     *
     */
    createEmptySegmentationForViewport: async _ref2 => {
      let {
        viewportId
      } = _ref2;
      const viewport = getTargetViewport({
        viewportId,
        viewportGridService
      });
      // Todo: add support for multiple display sets
      const displaySetInstanceUID = viewport.displaySetInstanceUIDs[0];
      const displaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);
      if (!displaySet.isReconstructable) {
        uiNotificationService.show({
          title: 'Segmentation',
          message: 'Segmentation is not supported for non-reconstructible displaysets yet',
          type: 'error'
        });
        return;
      }
      updateViewportsForSegmentationRendering({
        viewportId,
        servicesManager,
        loadFn: async () => {
          const currentSegmentations = segmentationService.getSegmentations();
          const segmentationId = await segmentationService.createSegmentationForDisplaySet(displaySetInstanceUID, {
            label: `Segmentation ${currentSegmentations.length + 1}`
          });
          const toolGroupId = viewport.viewportOptions.toolGroupId;
          await segmentationService.addSegmentationRepresentationToToolGroup(toolGroupId, segmentationId);

          // Add only one segment for now
          segmentationService.addSegment(segmentationId, {
            toolGroupId,
            segmentIndex: 1,
            properties: {
              label: 'Segment 1'
            }
          });
          return segmentationId;
        }
      });
    },
    /**
     * Loads segmentations for a specified viewport.
     * The function prepares the viewport for rendering, then loads the segmentation details.
     * Additionally, if the segmentation has scalar data, it is set for the corresponding label map volume.
     *
     * @param {Object} params - Parameters for the function.
     * @param params.segmentations - Array of segmentations to be loaded.
     * @param params.viewportId - the target viewport ID.
     *
     */
    loadSegmentationsForViewport: async _ref3 => {
      let {
        segmentations,
        viewportId
      } = _ref3;
      updateViewportsForSegmentationRendering({
        viewportId,
        servicesManager,
        loadFn: async () => {
          // Todo: handle adding more than one segmentation
          const viewport = getTargetViewport({
            viewportId,
            viewportGridService
          });
          const displaySetInstanceUID = viewport.displaySetInstanceUIDs[0];
          const segmentation = segmentations[0];
          const segmentationId = segmentation.id;
          const label = segmentation.label;
          const segments = segmentation.segments;
          delete segmentation.segments;
          await segmentationService.createSegmentationForDisplaySet(displaySetInstanceUID, {
            segmentationId,
            label
          });
          if (segmentation.scalarData) {
            const labelmapVolume = segmentationService.getLabelmapVolume(segmentationId);
            labelmapVolume.scalarData.set(segmentation.scalarData);
          }
          segmentationService.addOrUpdateSegmentation(segmentation);
          const toolGroupId = viewport.viewportOptions.toolGroupId;
          await segmentationService.addSegmentationRepresentationToToolGroup(toolGroupId, segmentationId);
          segments.forEach(segment => {
            if (segment === null) {
              return;
            }
            segmentationService.addSegment(segmentationId, {
              segmentIndex: segment.segmentIndex,
              toolGroupId,
              properties: {
                color: segment.color,
                label: segment.label,
                opacity: segment.opacity,
                isLocked: segment.isLocked,
                visibility: segment.isVisible,
                active: segmentation.activeSegmentIndex === segment.segmentIndex
              }
            });
          });
          if (segmentation.centroidsIJK) {
            segmentationService.setCentroids(segmentation.id, segmentation.centroidsIJK);
          }
          return segmentationId;
        }
      });
    },
    /**
     * Loads segmentation display sets for a specified viewport.
     * Depending on the modality of the display set (SEG or RTSTRUCT),
     * it chooses the appropriate service function to create
     * the segmentation for the display set.
     * The function then prepares the viewport for rendering segmentation.
     *
     * @param {Object} params - Parameters for the function.
     * @param params.viewportId - ID of the viewport where the segmentation display sets should be loaded.
     * @param params.displaySets - Array of display sets to be loaded for segmentation.
     *
     */
    loadSegmentationDisplaySetsForViewport: async _ref4 => {
      let {
        viewportId,
        displaySets
      } = _ref4;
      // Todo: handle adding more than one segmentation
      const displaySet = displaySets[0];
      updateViewportsForSegmentationRendering({
        viewportId,
        servicesManager,
        referencedDisplaySetInstanceUID: displaySet.referencedDisplaySetInstanceUID,
        loadFn: async () => {
          const segDisplaySet = displaySet;
          const suppressEvents = false;
          const serviceFunction = segDisplaySet.Modality === 'SEG' ? 'createSegmentationForSEGDisplaySet' : 'createSegmentationForRTDisplaySet';
          const boundFn = segmentationService[serviceFunction].bind(segmentationService);
          const segmentationId = await boundFn(segDisplaySet, null, suppressEvents);
          return segmentationId;
        }
      });
    },
    /**
     * Generates a segmentation from a given segmentation ID.
     * This function retrieves the associated segmentation and
     * its referenced volume, extracts label maps from the
     * segmentation volume, and produces segmentation data
     * alongside associated metadata.
     *
     * @param {Object} params - Parameters for the function.
     * @param params.segmentationId - ID of the segmentation to be generated.
     * @param params.options - Optional configuration for the generation process.
     *
     * @returns Returns the generated segmentation data.
     */
    generateSegmentation: _ref5 => {
      let {
        segmentationId,
        options = {}
      } = _ref5;
      const segmentation = dist_esm.segmentation.state.getSegmentation(segmentationId);
      const {
        referencedVolumeId
      } = segmentation.representationData.LABELMAP;
      const segmentationVolume = esm.cache.getVolume(segmentationId);
      const referencedVolume = esm.cache.getVolume(referencedVolumeId);
      const referencedImages = referencedVolume.getCornerstoneImages();
      const labelmapObj = generateLabelMaps2DFrom3D(segmentationVolume);

      // Generate fake metadata as an example
      labelmapObj.metadata = [];
      const segmentationInOHIF = segmentationService.getSegmentation(segmentationId);
      labelmapObj.segmentsOnLabelmap.forEach(segmentIndex => {
        // segmentation service already has a color for each segment
        const segment = segmentationInOHIF?.segments[segmentIndex];
        const {
          label,
          color
        } = segment;
        const RecommendedDisplayCIELabValue = dcmjs_es["default"].data.Colors.rgb2DICOMLAB(color.slice(0, 3).map(value => value / 255)).map(value => Math.round(value));
        const segmentMetadata = {
          SegmentNumber: segmentIndex.toString(),
          SegmentLabel: label,
          SegmentAlgorithmType: 'MANUAL',
          SegmentAlgorithmName: 'OHIF Brush',
          RecommendedDisplayCIELabValue,
          SegmentedPropertyCategoryCodeSequence: {
            CodeValue: 'T-D0050',
            CodingSchemeDesignator: 'SRT',
            CodeMeaning: 'Tissue'
          },
          SegmentedPropertyTypeCodeSequence: {
            CodeValue: 'T-D0050',
            CodingSchemeDesignator: 'SRT',
            CodeMeaning: 'Tissue'
          }
        };
        labelmapObj.metadata[segmentIndex] = segmentMetadata;
      });
      const generatedSegmentation = generateSegmentation(referencedImages, labelmapObj, esm.metaData, options);
      return generatedSegmentation;
    },
    /**
     * Downloads a segmentation based on the provided segmentation ID.
     * This function retrieves the associated segmentation and
     * uses it to generate the corresponding DICOM dataset, which
     * is then downloaded with an appropriate filename.
     *
     * @param {Object} params - Parameters for the function.
     * @param params.segmentationId - ID of the segmentation to be downloaded.
     *
     */
    downloadSegmentation: _ref6 => {
      let {
        segmentationId
      } = _ref6;
      const segmentationInOHIF = segmentationService.getSegmentation(segmentationId);
      const generatedSegmentation = actions.generateSegmentation({
        segmentationId
      });
      downloadDICOMData(generatedSegmentation.dataset, `${segmentationInOHIF.label}`);
    },
    /**
     * Stores a segmentation based on the provided segmentationId into a specified data source.
     * The SeriesDescription is derived from user input or defaults to the segmentation label,
     * and in its absence, defaults to 'Research Derived Series'.
     *
     * @param {Object} params - Parameters for the function.
     * @param params.segmentationId - ID of the segmentation to be stored.
     * @param params.dataSource - Data source where the generated segmentation will be stored.
     *
     * @returns {Object|void} Returns the naturalized report if successfully stored,
     * otherwise throws an error.
     */
    storeSegmentation: async _ref7 => {
      let {
        segmentationId,
        dataSource
      } = _ref7;
      const promptResult = await (0,default_src.createReportDialogPrompt)(uiDialogService, {
        extensionManager
      });
      if (promptResult.action !== 1 && promptResult.value) {
        return;
      }
      const segmentation = segmentationService.getSegmentation(segmentationId);
      if (!segmentation) {
        throw new Error('No segmentation found');
      }
      const {
        label
      } = segmentation;
      const SeriesDescription = promptResult.value || label || 'Research Derived Series';
      const generatedData = actions.generateSegmentation({
        segmentationId,
        options: {
          SeriesDescription
        }
      });
      if (!generatedData || !generatedData.dataset) {
        throw new Error('Error during segmentation generation');
      }
      const {
        dataset: naturalizedReport
      } = generatedData;
      await dataSource.store.dicom(naturalizedReport);

      // The "Mode" route listens for DicomMetadataStore changes
      // When a new instance is added, it listens and
      // automatically calls makeDisplaySets

      // add the information for where we stored it to the instance as well
      naturalizedReport.wadoRoot = dataSource.getConfig().wadoRoot;
      src.DicomMetadataStore.addInstances([naturalizedReport], true);
      return naturalizedReport;
    },
    /**
     * Converts segmentations into RTSS for download.
     * This sample function retrieves all segentations and passes to
     * cornerstone tool adapter to convert to DICOM RTSS format. It then
     * converts dataset to downloadable blob.
     *
     */
    downloadRTSS: _ref8 => {
      let {
        segmentationId
      } = _ref8;
      const segmentations = segmentationService.getSegmentation(segmentationId);
      const vtkUtils = {
        vtkImageMarchingSquares: ImageMarchingSquares/* default */.ZP,
        vtkDataArray: DataArray/* default */.ZP,
        vtkImageData: ImageData/* default */.ZP
      };
      const RTSS = generateRTSSFromSegmentations(segmentations, src.classes.MetadataProvider, src.DicomMetadataStore, esm.cache, dist_esm.Enums, vtkUtils);
      try {
        const reportBlob = datasetToBlob(RTSS);

        //Create a URL for the binary.
        const objectUrl = URL.createObjectURL(reportBlob);
        window.location.assign(objectUrl);
      } catch (e) {
        console.warn(e);
      }
    }
  };
  const definitions = {
    getUpdatedViewportsForSegmentation: {
      commandFn: actions.getUpdatedViewportsForSegmentation
    },
    loadSegmentationDisplaySetsForViewport: {
      commandFn: actions.loadSegmentationDisplaySetsForViewport
    },
    loadSegmentationsForViewport: {
      commandFn: actions.loadSegmentationsForViewport
    },
    createEmptySegmentationForViewport: {
      commandFn: actions.createEmptySegmentationForViewport
    },
    generateSegmentation: {
      commandFn: actions.generateSegmentation
    },
    downloadSegmentation: {
      commandFn: actions.downloadSegmentation
    },
    storeSegmentation: {
      commandFn: actions.storeSegmentation
    },
    downloadRTSS: {
      commandFn: actions.downloadRTSS
    }
  };
  return {
    actions,
    definitions
  };
};
/* harmony default export */ const src_commandsModule = (commandsModule);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/init.ts

function init(_ref) {
  let {
    configuration = {}
  } = _ref;
  (0,dist_esm.addTool)(dist_esm.BrushTool);
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/index.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }







const Component = /*#__PURE__*/react.lazy(() => {
  return __webpack_require__.e(/* import() */ 451).then(__webpack_require__.bind(__webpack_require__, 4451));
});
const OHIFCornerstoneSEGViewport = props => {
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
  preRegistration: init,
  /**
   * PanelModule should provide a list of panels that will be available in OHIF
   * for Modes to consume and render. Each panel is defined by a {name,
   * iconName, iconLabel, label, component} object. Example of a panel module
   * is the StudyBrowserPanel that is provided by the default extension in OHIF.
   */
  getPanelModule: src_getPanelModule,
  getCommandsModule: src_commandsModule,
  getViewportModule(_ref) {
    let {
      servicesManager,
      extensionManager
    } = _ref;
    const ExtendedOHIFCornerstoneSEGViewport = props => {
      return /*#__PURE__*/react.createElement(OHIFCornerstoneSEGViewport, _extends({
        servicesManager: servicesManager,
        extensionManager: extensionManager,
        commandsManager: commandsManager
      }, props));
    };
    return [{
      name: 'dicom-seg',
      component: ExtendedOHIFCornerstoneSEGViewport
    }];
  },
  /**
   * SopClassHandlerModule should provide a list of sop class handlers that will be
   * available in OHIF for Modes to consume and use to create displaySets from Series.
   * Each sop class handler is defined by a { name, sopClassUids, getDisplaySetsFromSeries}.
   * Examples include the default sop class handler provided by the default extension
   */
  getSopClassHandlerModule: src_getSopClassHandlerModule,
  getHangingProtocolModule: src_getHangingProtocolModule
};
/* harmony default export */ const cornerstone_dicom_seg_src = (extension);

/***/ }),

/***/ 78753:
/***/ (() => {

/* (ignored) */

/***/ })

}]);