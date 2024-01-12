"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[451],{

/***/ 4451:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ viewports_OHIFCornerstoneSEGViewport)
});

// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../../node_modules/react-i18next/dist/es/index.js + 15 modules
var es = __webpack_require__(69190);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/utils/initSEGToolGroup.ts
function createSEGToolGroupAndAddTools(ToolGroupService, customizationService, toolGroupId) {
  const {
    tools
  } = customizationService.get('cornerstone.overlayViewportTools') ?? {};
  return ToolGroupService.createToolGroupAndAddTools(toolGroupId, tools);
}
/* harmony default export */ const initSEGToolGroup = (createSEGToolGroupAndAddTools);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/utils/promptHydrateSEG.ts

const RESPONSE = {
  NO_NEVER: -1,
  CANCEL: 0,
  HYDRATE_SEG: 5
};
function promptHydrateSEG(_ref) {
  let {
    servicesManager,
    segDisplaySet,
    viewportId,
    preHydrateCallbacks,
    hydrateSEGDisplaySet
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
      const isHydrated = await hydrateSEGDisplaySet({
        segDisplaySet,
        viewportId
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
/* harmony default export */ const utils_promptHydrateSEG = (promptHydrateSEG);
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/viewports/_getStatusComponent.tsx



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
      ToolTipMessage = () => /*#__PURE__*/react.createElement("div", null, "Click LOAD to load segmentation.");
  }
  const StatusArea = () => /*#__PURE__*/react.createElement("div", {
    className: "flex h-6 cursor-default text-sm leading-6 text-white"
  }, /*#__PURE__*/react.createElement("div", {
    className: "bg-customgray-100 flex min-w-[45px] items-center rounded-l-xl rounded-r p-1"
  }, /*#__PURE__*/react.createElement(StatusIcon, null), /*#__PURE__*/react.createElement("span", {
    className: "ml-1"
  }, "SEG")), !isHydrated && /*#__PURE__*/react.createElement("div", {
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
;// CONCATENATED MODULE: ../../../extensions/cornerstone-dicom-seg/src/viewports/OHIFCornerstoneSEGViewport.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }








const {
  formatDate
} = src.utils;
const SEG_TOOLGROUP_BASE_NAME = 'SEGToolGroup';
function OHIFCornerstoneSEGViewport(props) {
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
    t
  } = (0,es/* useTranslation */.$G)('SEGViewport');
  const viewportId = viewportOptions.viewportId;
  const {
    displaySetService,
    toolGroupService,
    segmentationService,
    uiNotificationService,
    customizationService
  } = servicesManager.services;
  const toolGroupId = `${SEG_TOOLGROUP_BASE_NAME}-${viewportId}`;

  // SEG viewport will always have a single display set
  if (displaySets.length > 1) {
    throw new Error('SEG viewport should only have a single display set');
  }
  const segDisplaySet = displaySets[0];
  const [viewportGrid, viewportGridService] = (0,ui_src/* useViewportGrid */.O_)();

  // States
  const [selectedSegment, setSelectedSegment] = (0,react.useState)(1);

  // Hydration means that the SEG is opened and segments are loaded into the
  // segmentation panel, and SEG is also rendered on any viewport that is in the
  // same frameOfReferenceUID as the referencedSeriesUID of the SEG. However,
  // loading basically means SEG loading over network and bit unpacking of the
  // SEG data.
  const [isHydrated, setIsHydrated] = (0,react.useState)(segDisplaySet.isHydrated);
  const [segIsLoading, setSegIsLoading] = (0,react.useState)(!segDisplaySet.isLoaded);
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
  const referencedDisplaySet = segDisplaySet.getReferenceDisplaySet();
  const referencedDisplaySetMetadata = _getReferencedDisplaySetMetadata(referencedDisplaySet, segDisplaySet);
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
  const getCornerstoneViewport = (0,react.useCallback)(() => {
    const {
      component: Component
    } = extensionManager.getModuleEntry('@ohif/extension-cornerstone.viewportModule.cornerstone');
    const {
      displaySet: referencedDisplaySet
    } = referencedDisplaySetRef.current;

    // Todo: jump to the center of the first segment
    return /*#__PURE__*/react.createElement(Component, _extends({}, props, {
      displaySets: [referencedDisplaySet, segDisplaySet],
      viewportOptions: {
        viewportType: 'volume',
        toolGroupId: toolGroupId,
        orientation: viewportOptions.orientation,
        viewportId: viewportOptions.viewportId
      },
      onElementEnabled: onElementEnabled,
      onElementDisabled: onElementDisabled
      // initialImageIndex={initialImageIndex}
    }));
  }, [viewportId, segDisplaySet, toolGroupId]);
  const onSegmentChange = (0,react.useCallback)(direction => {
    direction = direction === 'left' ? -1 : 1;
    const segmentationId = segDisplaySet.displaySetInstanceUID;
    const segmentation = segmentationService.getSegmentation(segmentationId);
    const {
      segments
    } = segmentation;
    const numberOfSegments = Object.keys(segments).length;
    let newSelectedSegmentIndex = selectedSegment + direction;

    // Segment 0 is always background

    if (newSelectedSegmentIndex > numberOfSegments - 1) {
      newSelectedSegmentIndex = 1;
    } else if (newSelectedSegmentIndex === 0) {
      newSelectedSegmentIndex = numberOfSegments - 1;
    }
    segmentationService.jumpToSegmentCenter(segmentationId, newSelectedSegmentIndex, toolGroupId);
    setSelectedSegment(newSelectedSegmentIndex);
  }, [selectedSegment]);
  (0,react.useEffect)(() => {
    if (segIsLoading) {
      return;
    }
    utils_promptHydrateSEG({
      servicesManager,
      viewportId,
      segDisplaySet,
      preHydrateCallbacks: [storePresentationState],
      hydrateSEGDisplaySet
    }).then(isHydrated => {
      if (isHydrated) {
        setIsHydrated(true);
      }
    });
  }, [servicesManager, viewportId, segDisplaySet, segIsLoading]);
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = segmentationService.subscribe(segmentationService.EVENTS.SEGMENTATION_LOADING_COMPLETE, evt => {
      if (evt.segDisplaySet.displaySetInstanceUID === segDisplaySet.displaySetInstanceUID) {
        setSegIsLoading(false);
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
  }, [segDisplaySet]);
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = segmentationService.subscribe(segmentationService.EVENTS.SEGMENT_LOADING_COMPLETE, _ref2 => {
      let {
        percentComplete,
        numSegments
      } = _ref2;
      setProcessingProgress({
        percentComplete,
        totalSegments: numSegments
      });
    });
    return () => {
      unsubscribe();
    };
  }, [segDisplaySet]);

  /**
   Cleanup the SEG viewport when the viewport is destroyed
   */
  (0,react.useEffect)(() => {
    const onDisplaySetsRemovedSubscription = displaySetService.subscribe(displaySetService.EVENTS.DISPLAY_SETS_REMOVED, _ref3 => {
      let {
        displaySetInstanceUIDs
      } = _ref3;
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

    // This creates a custom tool group which has the lifetime of this view
    // only, and does NOT interfere with currently displayed segmentations.
    toolGroup = initSEGToolGroup(toolGroupService, customizationService, toolGroupId);
    return () => {
      // remove the segmentation representations if seg displayset changed
      segmentationService.removeSegmentationRepresentationFromToolGroup(toolGroupId);

      // Only destroy the viewport specific implementation
      toolGroupService.destroyToolGroup(toolGroupId);
    };
  }, []);
  (0,react.useEffect)(() => {
    setIsHydrated(segDisplaySet.isHydrated);
    return () => {
      // remove the segmentation representations if seg displayset changed
      segmentationService.removeSegmentationRepresentationFromToolGroup(toolGroupId);
      referencedDisplaySetRef.current = null;
    };
  }, [segDisplaySet]);

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
    SpacingBetweenSlices
  } = referencedDisplaySetRef.current.metadata;
  const hydrateSEGDisplaySet = _ref4 => {
    let {
      segDisplaySet,
      viewportId
    } = _ref4;
    commandsManager.runCommand('loadSegmentationDisplaySetsForViewport', {
      displaySets: [segDisplaySet],
      viewportId
    });
  };
  const onStatusClick = async () => {
    // Before hydrating a SEG and make it added to all viewports in the grid
    // that share the same frameOfReferenceUID, we need to store the viewport grid
    // presentation state, so that we can restore it after hydrating the SEG. This is
    // required if the user has changed the viewport (other viewport than SEG viewport)
    // presentation state (w/l and invert) and then opens the SEG. If we don't store
    // the presentation state, the viewport will be reset to the default presentation
    storePresentationState();
    const isHydrated = await hydrateSEGDisplaySet({
      segDisplaySet,
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
      seriesDescription: `SEG Viewport ${SeriesDescription}`,
      patientInformation: {
        patientName: PatientName ? src["default"].utils.formatPN(PatientName.Alphabetic) : '',
        patientSex: PatientSex || '',
        patientAge: PatientAge || '',
        MRN: PatientID || '',
        thickness: SliceThickness ? src.utils.roundNumber(SliceThickness, 2) : '',
        thicknessUnits: SliceThickness !== undefined ? 'mm' : '',
        spacing: SpacingBetweenSlices !== undefined ? src.utils.roundNumber(SpacingBetweenSlices, 2) : '',
        scanner: ManufacturerModelName || ''
      }
    }
  }), /*#__PURE__*/react.createElement("div", {
    className: "relative flex h-full w-full flex-row overflow-hidden"
  }, segIsLoading && /*#__PURE__*/react.createElement(ui_src/* LoadingIndicatorTotalPercent */.bk, {
    className: "h-full w-full",
    totalNumbers: processingProgress.totalSegments,
    percentComplete: processingProgress.percentComplete,
    loadingText: "Loading SEG..."
  }), getCornerstoneViewport(), childrenWithProps));
}
OHIFCornerstoneSEGViewport.propTypes = {
  displaySets: prop_types_default().arrayOf((prop_types_default()).object),
  viewportId: (prop_types_default()).string.isRequired,
  dataSource: (prop_types_default()).object,
  children: (prop_types_default()).node,
  customProps: (prop_types_default()).object
};
OHIFCornerstoneSEGViewport.defaultProps = {
  customProps: {}
};
function _getReferencedDisplaySetMetadata(referencedDisplaySet, segDisplaySet) {
  const {
    SharedFunctionalGroupsSequence
  } = segDisplaySet.instance;
  const SharedFunctionalGroup = Array.isArray(SharedFunctionalGroupsSequence) ? SharedFunctionalGroupsSequence[0] : SharedFunctionalGroupsSequence;
  const {
    PixelMeasuresSequence
  } = SharedFunctionalGroup;
  const PixelMeasures = Array.isArray(PixelMeasuresSequence) ? PixelMeasuresSequence[0] : PixelMeasuresSequence;
  const {
    SpacingBetweenSlices,
    SliceThickness
  } = PixelMeasures;
  const image0 = referencedDisplaySet.images[0];
  const referencedDisplaySetMetadata = {
    PatientID: image0.PatientID,
    PatientName: image0.PatientName,
    PatientSex: image0.PatientSex,
    PatientAge: image0.PatientAge,
    SliceThickness: image0.SliceThickness || SliceThickness,
    StudyDate: image0.StudyDate,
    SeriesDescription: image0.SeriesDescription,
    SeriesInstanceUID: image0.SeriesInstanceUID,
    SeriesNumber: image0.SeriesNumber,
    ManufacturerModelName: image0.ManufacturerModelName,
    SpacingBetweenSlices: image0.SpacingBetweenSlices || SpacingBetweenSlices
  };
  return referencedDisplaySetMetadata;
}
/* harmony default export */ const viewports_OHIFCornerstoneSEGViewport = (OHIFCornerstoneSEGViewport);

/***/ })

}]);