"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[471],{

/***/ 56471:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ viewports_OHIFCornerstoneRTViewport)
});

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/src/utils/promptHydrateRT.ts

const RESPONSE = {
  NO_NEVER: -1,
  CANCEL: 0,
  HYDRATE_SEG: 5
};
function promptHydrateRT(_ref) {
  let {
    servicesManager,
    rtDisplaySet,
    viewportId,
    toolGroupId = 'default',
    preHydrateCallbacks,
    hydrateRTDisplaySet
  } = _ref;
  const {
    uiViewportDialogService
  } = servicesManager.services;
  return new Promise(async function (resolve, reject) {
    const promptResult = await _askHydrate(uiViewportDialogService, viewportId);
    if (promptResult === RESPONSE.HYDRATE_SEG) {
      preHydrateCallbacks?.forEach(callback => {
        callback();
      });
      const isHydrated = await hydrateRTDisplaySet({
        rtDisplaySet,
        viewportId,
        toolGroupId,
        servicesManager
      });
      resolve(isHydrated);
    }
  });
}
function _askHydrate(uiViewportDialogService, viewportId) {
  return new Promise(function (resolve, reject) {
    const message = 'Do you want to open this Segmentation?';
    const actions = [{
      type: ui_src/* ButtonEnums.type */.LZ.dt.secondary,
      text: 'No',
      value: RESPONSE.CANCEL
    }, {
      type: ui_src/* ButtonEnums.type */.LZ.dt.primary,
      text: 'Yes',
      value: RESPONSE.HYDRATE_SEG
    }];
    const onSubmit = result => {
      uiViewportDialogService.hide();
      resolve(result);
    };
    uiViewportDialogService.show({
      viewportId,
      type: 'info',
      message,
      actions,
      onSubmit,
      onOutsideClick: () => {
        uiViewportDialogService.hide();
        resolve(RESPONSE.CANCEL);
      }
    });
  });
}
/* harmony default export */ const utils_promptHydrateRT = (promptHydrateRT);
// EXTERNAL MODULE: ../../../node_modules/react-i18next/dist/es/index.js + 15 modules
var es = __webpack_require__(69190);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/src/viewports/_getStatusComponent.tsx



function _getStatusComponent(_ref) {
  let {
    isHydrated,
    onStatusClick
  } = _ref;
  let ToolTipMessage = null;
  let StatusIcon = null;
  const {
    t
  } = (0,es/* useTranslation */.$G)('Common');
  const loadStr = t('LOAD');
  switch (isHydrated) {
    case true:
      StatusIcon = () => /*#__PURE__*/react.createElement(ui_src/* Icon */.JO, {
        name: "status-alert"
      });
      ToolTipMessage = () => /*#__PURE__*/react.createElement("div", null, "This Segmentation is loaded in the segmentation panel");
      break;
    case false:
      StatusIcon = () => /*#__PURE__*/react.createElement(ui_src/* Icon */.JO, {
        className: "text-aqua-pale",
        name: "status-untracked"
      });
      ToolTipMessage = () => /*#__PURE__*/react.createElement("div", null, "Click LOAD to load RTSTRUCT.");
  }
  const StatusArea = () => /*#__PURE__*/react.createElement("div", {
    className: "flex h-6 cursor-default text-sm leading-6 text-white"
  }, /*#__PURE__*/react.createElement("div", {
    className: "bg-customgray-100 flex min-w-[45px] items-center rounded-l-xl rounded-r p-1"
  }, /*#__PURE__*/react.createElement(StatusIcon, null), /*#__PURE__*/react.createElement("span", {
    className: "ml-1"
  }, "RTSTRUCT")), !isHydrated && /*#__PURE__*/react.createElement("div", {
    className: "bg-primary-main hover:bg-primary-light ml-1 cursor-pointer rounded px-1.5 hover:text-black"
    // Using onMouseUp here because onClick is not working when the viewport is not active and is styled with pointer-events:none
    ,
    onMouseUp: onStatusClick
  }, loadStr));
  return /*#__PURE__*/react.createElement(react.Fragment, null, ToolTipMessage && /*#__PURE__*/react.createElement(ui_src/* Tooltip */.u, {
    content: /*#__PURE__*/react.createElement(ToolTipMessage, null),
    position: "bottom-left"
  }, /*#__PURE__*/react.createElement(StatusArea, null)), !ToolTipMessage && /*#__PURE__*/react.createElement(StatusArea, null));
}
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/src/utils/initRTToolGroup.ts
function createRTToolGroupAndAddTools(ToolGroupService, customizationService, toolGroupId) {
  const {
    tools
  } = customizationService.get('cornerstone.overlayViewportTools') ?? {};
  return ToolGroupService.createToolGroupAndAddTools(toolGroupId, tools);
}
/* harmony default export */ const initRTToolGroup = (createRTToolGroupAndAddTools);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-rt/src/viewports/OHIFCornerstoneRTViewport.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }







const {
  formatDate
} = src.utils;
const RT_TOOLGROUP_BASE_NAME = 'RTToolGroup';
function OHIFCornerstoneRTViewport(props) {
  const {
    children,
    displaySets,
    viewportOptions,
    viewportLabel,
    servicesManager,
    extensionManager,
    commandsManager
  } = props;
  const {
    displaySetService,
    toolGroupService,
    segmentationService,
    uiNotificationService,
    customizationService
  } = servicesManager.services;
  const viewportId = viewportOptions.viewportId;
  const toolGroupId = `${RT_TOOLGROUP_BASE_NAME}-${viewportId}`;

  // RT viewport will always have a single display set
  if (displaySets.length > 1) {
    throw new Error('RT viewport should only have a single display set');
  }
  const rtDisplaySet = displaySets[0];
  const [viewportGrid, viewportGridService] = (0,ui_src/* useViewportGrid */.O_)();

  // States
  const [isToolGroupCreated, setToolGroupCreated] = (0,react.useState)(false);
  const [selectedSegment, setSelectedSegment] = (0,react.useState)(1);

  // Hydration means that the RT is opened and segments are loaded into the
  // segmentation panel, and RT is also rendered on any viewport that is in the
  // same frameOfReferenceUID as the referencedSeriesUID of the RT. However,
  // loading basically means RT loading over network and bit unpacking of the
  // RT data.
  const [isHydrated, setIsHydrated] = (0,react.useState)(rtDisplaySet.isHydrated);
  const [rtIsLoading, setRtIsLoading] = (0,react.useState)(!rtDisplaySet.isLoaded);
  const [element, setElement] = (0,react.useState)(null);
  const [processingProgress, setProcessingProgress] = (0,react.useState)({
    percentComplete: null,
    totalSegments: null
  });

  // refs
  const referencedDisplaySetRef = (0,react.useRef)(null);
  const {
    viewports,
    activeViewportId
  } = viewportGrid;
  const referencedDisplaySet = rtDisplaySet.getReferenceDisplaySet();
  const referencedDisplaySetMetadata = _getReferencedDisplaySetMetadata(referencedDisplaySet);
  referencedDisplaySetRef.current = {
    displaySet: referencedDisplaySet,
    metadata: referencedDisplaySetMetadata
  };
  /**
   * OnElementEnabled callback which is called after the cornerstoneExtension
   * has enabled the element. Note: we delegate all the image rendering to
   * cornerstoneExtension, so we don't need to do anything here regarding
   * the image rendering, element enabling etc.
   */
  const onElementEnabled = evt => {
    setElement(evt.detail.element);
  };
  const onElementDisabled = () => {
    setElement(null);
  };
  const storePresentationState = (0,react.useCallback)(() => {
    viewportGrid?.viewports.forEach(_ref => {
      let {
        viewportId
      } = _ref;
      commandsManager.runCommand('storePresentation', {
        viewportId
      });
    });
  }, [viewportGrid]);
  const hydrateRTDisplaySet = _ref2 => {
    let {
      rtDisplaySet,
      viewportId
    } = _ref2;
    commandsManager.runCommand('loadSegmentationDisplaySetsForViewport', {
      displaySets: [rtDisplaySet],
      viewportId
    });
  };
  const getCornerstoneViewport = (0,react.useCallback)(() => {
    const {
      component: Component
    } = extensionManager.getModuleEntry('@ohif/extension-cornerstone.viewportModule.cornerstone');
    const {
      displaySet: referencedDisplaySet
    } = referencedDisplaySetRef.current;

    // Todo: jump to the center of the first segment
    return /*#__PURE__*/react.createElement(Component, _extends({}, props, {
      displaySets: [referencedDisplaySet, rtDisplaySet],
      viewportOptions: {
        viewportType: 'volume',
        toolGroupId: toolGroupId,
        orientation: viewportOptions.orientation,
        viewportId: viewportOptions.viewportId
      },
      onElementEnabled: onElementEnabled,
      onElementDisabled: onElementDisabled
    }));
  }, [viewportId, rtDisplaySet, toolGroupId]);
  const onSegmentChange = (0,react.useCallback)(direction => {
    direction = direction === 'left' ? -1 : 1;
    const segmentationId = rtDisplaySet.displaySetInstanceUID;
    const segmentation = segmentationService.getSegmentation(segmentationId);
    const {
      segments
    } = segmentation;
    const numberOfSegments = Object.keys(segments).length;
    let newSelectedSegmentIndex = selectedSegment + direction;

    // Segment 0 is always background
    if (newSelectedSegmentIndex >= numberOfSegments - 1) {
      newSelectedSegmentIndex = 1;
    } else if (newSelectedSegmentIndex === 0) {
      newSelectedSegmentIndex = numberOfSegments - 1;
    }
    segmentationService.jumpToSegmentCenter(segmentationId, newSelectedSegmentIndex, toolGroupId);
    setSelectedSegment(newSelectedSegmentIndex);
  }, [selectedSegment]);
  (0,react.useEffect)(() => {
    if (rtIsLoading) {
      return;
    }
    utils_promptHydrateRT({
      servicesManager,
      viewportId,
      rtDisplaySet,
      preHydrateCallbacks: [storePresentationState],
      hydrateRTDisplaySet
    }).then(isHydrated => {
      if (isHydrated) {
        setIsHydrated(true);
      }
    });
  }, [servicesManager, viewportId, rtDisplaySet, rtIsLoading]);
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = segmentationService.subscribe(segmentationService.EVENTS.SEGMENTATION_LOADING_COMPLETE, evt => {
      if (evt.rtDisplaySet.displaySetInstanceUID === rtDisplaySet.displaySetInstanceUID) {
        setRtIsLoading(false);
      }
      if (evt.overlappingSegments) {
        uiNotificationService.show({
          title: 'Overlapping Segments',
          message: 'Overlapping segments detected which is not currently supported',
          type: 'warning'
        });
      }
    });
    return () => {
      unsubscribe();
    };
  }, [rtDisplaySet]);
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = segmentationService.subscribe(segmentationService.EVENTS.SEGMENT_LOADING_COMPLETE, _ref3 => {
      let {
        percentComplete,
        numSegments
      } = _ref3;
      setProcessingProgress({
        percentComplete,
        totalSegments: numSegments
      });
    });
    return () => {
      unsubscribe();
    };
  }, [rtDisplaySet]);

  /**
   Cleanup the SEG viewport when the viewport is destroyed
   */
  (0,react.useEffect)(() => {
    const onDisplaySetsRemovedSubscription = displaySetService.subscribe(displaySetService.EVENTS.DISPLAY_SETS_REMOVED, _ref4 => {
      let {
        displaySetInstanceUIDs
      } = _ref4;
      const activeViewport = viewports.get(activeViewportId);
      if (displaySetInstanceUIDs.includes(activeViewport.displaySetInstanceUID)) {
        viewportGridService.setDisplaySetsForViewport({
          viewportId: activeViewportId,
          displaySetInstanceUIDs: []
        });
      }
    });
    return () => {
      onDisplaySetsRemovedSubscription.unsubscribe();
    };
  }, []);
  (0,react.useEffect)(() => {
    let toolGroup = toolGroupService.getToolGroup(toolGroupId);
    if (toolGroup) {
      return;
    }
    toolGroup = initRTToolGroup(toolGroupService, customizationService, toolGroupId);
    setToolGroupCreated(true);
    return () => {
      // remove the segmentation representations if seg displayset changed
      segmentationService.removeSegmentationRepresentationFromToolGroup(toolGroupId);
      toolGroupService.destroyToolGroup(toolGroupId);
    };
  }, []);
  (0,react.useEffect)(() => {
    setIsHydrated(rtDisplaySet.isHydrated);
    return () => {
      // remove the segmentation representations if seg displayset changed
      segmentationService.removeSegmentationRepresentationFromToolGroup(toolGroupId);
      referencedDisplaySetRef.current = null;
    };
  }, [rtDisplaySet]);

  // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
  let childrenWithProps = null;
  if (!referencedDisplaySetRef.current || referencedDisplaySet.displaySetInstanceUID !== referencedDisplaySetRef.current.displaySet.displaySetInstanceUID) {
    return null;
  }
  if (children && children.length) {
    childrenWithProps = children.map((child, index) => {
      return child && /*#__PURE__*/react.cloneElement(child, {
        viewportId,
        key: index
      });
    });
  }
  const {
    PatientID,
    PatientName,
    PatientSex,
    PatientAge,
    SliceThickness,
    ManufacturerModelName,
    StudyDate,
    SeriesDescription,
    SpacingBetweenSlices,
    SeriesNumber
  } = referencedDisplaySetRef.current.metadata;
  const onStatusClick = async () => {
    // Before hydrating a RT and make it added to all viewports in the grid
    // that share the same frameOfReferenceUID, we need to store the viewport grid
    // presentation state, so that we can restore it after hydrating the RT. This is
    // required if the user has changed the viewport (other viewport than RT viewport)
    // presentation state (w/l and invert) and then opens the RT. If we don't store
    // the presentation state, the viewport will be reset to the default presentation
    storePresentationState();
    const isHydrated = await hydrateRTDisplaySet({
      rtDisplaySet,
      viewportId
    });
    setIsHydrated(isHydrated);
  };
  return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement(ui_src/* ViewportActionBar */.uY, {
    onDoubleClick: evt => {
      evt.stopPropagation();
      evt.preventDefault();
    },
    onArrowsClick: onSegmentChange,
    getStatusComponent: () => {
      return _getStatusComponent({
        isHydrated,
        onStatusClick
      });
    },
    studyData: {
      label: viewportLabel,
      useAltStyling: true,
      studyDate: formatDate(StudyDate),
      currentSeries: SeriesNumber,
      seriesDescription: `RT Viewport ${SeriesDescription}`,
      patientInformation: {
        patientName: PatientName ? src["default"].utils.formatPN(PatientName.Alphabetic) : '',
        patientSex: PatientSex || '',
        patientAge: PatientAge || '',
        MRN: PatientID || '',
        thickness: SliceThickness ? `${SliceThickness.toFixed(2)}mm` : '',
        spacing: SpacingBetweenSlices !== undefined ? `${SpacingBetweenSlices.toFixed(2)}mm` : '',
        scanner: ManufacturerModelName || ''
      }
    }
  }), /*#__PURE__*/react.createElement("div", {
    className: "relative flex h-full w-full flex-row overflow-hidden"
  }, rtIsLoading && /*#__PURE__*/react.createElement(ui_src/* LoadingIndicatorTotalPercent */.bk, {
    className: "h-full w-full",
    totalNumbers: processingProgress.totalSegments,
    percentComplete: processingProgress.percentComplete,
    loadingText: "Loading RTSTRUCT..."
  }), getCornerstoneViewport(), childrenWithProps));
}
OHIFCornerstoneRTViewport.propTypes = {
  displaySets: prop_types_default().arrayOf((prop_types_default()).object),
  viewportId: (prop_types_default()).string.isRequired,
  dataSource: (prop_types_default()).object,
  children: (prop_types_default()).node,
  customProps: (prop_types_default()).object
};
OHIFCornerstoneRTViewport.defaultProps = {
  customProps: {}
};
function _getReferencedDisplaySetMetadata(referencedDisplaySet) {
  const image0 = referencedDisplaySet.images[0];
  const referencedDisplaySetMetadata = {
    PatientID: image0.PatientID,
    PatientName: image0.PatientName,
    PatientSex: image0.PatientSex,
    PatientAge: image0.PatientAge,
    SliceThickness: image0.SliceThickness,
    StudyDate: image0.StudyDate,
    SeriesDescription: image0.SeriesDescription,
    SeriesInstanceUID: image0.SeriesInstanceUID,
    SeriesNumber: image0.SeriesNumber,
    ManufacturerModelName: image0.ManufacturerModelName,
    SpacingBetweenSlices: image0.SpacingBetweenSlices
  };
  return referencedDisplaySetMetadata;
}
/* harmony default export */ const viewports_OHIFCornerstoneRTViewport = (OHIFCornerstoneRTViewport);

/***/ })

}]);