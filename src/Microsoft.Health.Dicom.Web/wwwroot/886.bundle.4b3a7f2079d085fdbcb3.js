"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[886],{

/***/ 48886:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (__WEBPACK_DEFAULT_EXPORT__)
/* harmony export */ });
/* harmony import */ var prop_types__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(3827);
/* harmony import */ var prop_types__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(prop_types__WEBPACK_IMPORTED_MODULE_0__);
/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(43001);
/* harmony import */ var react_i18next__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(69190);
/* harmony import */ var _ohif_core__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(71771);
/* harmony import */ var _tools_modules_dicomSRModule__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(64035);
/* harmony import */ var _ohif_ui__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(71783);
/* harmony import */ var _utils_hydrateStructuredReport__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(38965);
/* harmony import */ var _state__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(62657);
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }








const {
  formatDate
} = _ohif_core__WEBPACK_IMPORTED_MODULE_3__.utils;
const MEASUREMENT_TRACKING_EXTENSION_ID = '@ohif/extension-measurement-tracking';
const SR_TOOLGROUP_BASE_NAME = 'SRToolGroup';
function OHIFCornerstoneSRViewport(props) {
  const {
    children,
    dataSource,
    displaySets,
    viewportLabel,
    viewportOptions,
    servicesManager,
    extensionManager
  } = props;
  const [appConfig] = (0,_state__WEBPACK_IMPORTED_MODULE_7__/* .useAppConfig */ .M)();
  const {
    displaySetService,
    cornerstoneViewportService,
    measurementService
  } = servicesManager.services;
  const viewportId = viewportOptions.viewportId;

  // SR viewport will always have a single display set
  if (displaySets.length > 1) {
    throw new Error('SR viewport should only have a single display set');
  }
  const srDisplaySet = displaySets[0];
  const [viewportGrid, viewportGridService] = (0,_ohif_ui__WEBPACK_IMPORTED_MODULE_5__/* .useViewportGrid */ .O_)();
  const [measurementSelected, setMeasurementSelected] = (0,react__WEBPACK_IMPORTED_MODULE_1__.useState)(0);
  const [measurementCount, setMeasurementCount] = (0,react__WEBPACK_IMPORTED_MODULE_1__.useState)(1);
  const [activeImageDisplaySetData, setActiveImageDisplaySetData] = (0,react__WEBPACK_IMPORTED_MODULE_1__.useState)(null);
  const [referencedDisplaySetMetadata, setReferencedDisplaySetMetadata] = (0,react__WEBPACK_IMPORTED_MODULE_1__.useState)(null);
  const [element, setElement] = (0,react__WEBPACK_IMPORTED_MODULE_1__.useState)(null);
  const {
    viewports,
    activeViewportId
  } = viewportGrid;

  // Optional hook into tracking extension, if present.
  let trackedMeasurements;
  let sendTrackedMeasurementsEvent;
  const hasMeasurementTrackingExtension = extensionManager.registeredExtensionIds.includes(MEASUREMENT_TRACKING_EXTENSION_ID);
  if (hasMeasurementTrackingExtension) {
    const contextModule = extensionManager.getModuleEntry('@ohif/extension-measurement-tracking.contextModule.TrackedMeasurementsContext');
    const tracked = (0,react__WEBPACK_IMPORTED_MODULE_1__.useContext)(contextModule.context);
    trackedMeasurements = tracked?.[0];
    sendTrackedMeasurementsEvent = tracked?.[1];
  }
  if (!sendTrackedMeasurementsEvent) {
    // if no panels from measurement-tracking extension is used, this code will run
    trackedMeasurements = null;
    sendTrackedMeasurementsEvent = (eventName, _ref) => {
      let {
        displaySetInstanceUID
      } = _ref;
      measurementService.clearMeasurements();
      const {
        SeriesInstanceUIDs
      } = (0,_utils_hydrateStructuredReport__WEBPACK_IMPORTED_MODULE_6__/* ["default"] */ .Z)({
        servicesManager,
        extensionManager,
        appConfig
      }, displaySetInstanceUID);
      const displaySets = displaySetService.getDisplaySetsForSeries(SeriesInstanceUIDs[0]);
      if (displaySets.length) {
        viewportGridService.setDisplaySetsForViewports([{
          viewportId: activeViewportId,
          displaySetInstanceUIDs: [displaySets[0].displaySetInstanceUID]
        }]);
      }
    };
  }

  /**
   * Store the tracking identifiers per viewport in order to be able to
   * show the SR measurements on the referenced image on the correct viewport,
   * when multiple viewports are used.
   */
  const setTrackingIdentifiers = (0,react__WEBPACK_IMPORTED_MODULE_1__.useCallback)(measurementSelected => {
    const {
      measurements
    } = srDisplaySet;
    (0,_tools_modules_dicomSRModule__WEBPACK_IMPORTED_MODULE_4__/* .setTrackingUniqueIdentifiersForElement */ .l2)(element, measurements.map(measurement => measurement.TrackingUniqueIdentifier), measurementSelected);
  }, [element, measurementSelected, srDisplaySet]);

  /**
   * OnElementEnabled callback which is called after the cornerstoneExtension
   * has enabled the element. Note: we delegate all the image rendering to
   * cornerstoneExtension, so we don't need to do anything here regarding
   * the image rendering, element enabling etc.
   */
  const onElementEnabled = evt => {
    setElement(evt.detail.element);
  };
  const updateViewport = (0,react__WEBPACK_IMPORTED_MODULE_1__.useCallback)(newMeasurementSelected => {
    const {
      StudyInstanceUID,
      displaySetInstanceUID,
      sopClassUids
    } = srDisplaySet;
    if (!StudyInstanceUID || !displaySetInstanceUID) {
      return;
    }
    if (sopClassUids && sopClassUids.length > 1) {
      // Todo: what happens if there are multiple SOP Classes? Why we are
      // not throwing an error?
      console.warn('More than one SOPClassUID in the same series is not yet supported.');
    }
    _getViewportReferencedDisplaySetData(srDisplaySet, newMeasurementSelected, displaySetService).then(_ref2 => {
      let {
        referencedDisplaySet,
        referencedDisplaySetMetadata
      } = _ref2;
      setMeasurementSelected(newMeasurementSelected);
      setActiveImageDisplaySetData(referencedDisplaySet);
      setReferencedDisplaySetMetadata(referencedDisplaySetMetadata);
      if (referencedDisplaySet.displaySetInstanceUID === activeImageDisplaySetData?.displaySetInstanceUID) {
        const {
          measurements
        } = srDisplaySet;

        // it means that we have a new referenced display set, and the
        // imageIdIndex will handle it by updating the viewport, but if they
        // are the same we just need to use measurementService to jump to the
        // new measurement
        const csViewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
        const imageIds = csViewport.getImageIds();
        const imageIdIndex = imageIds.indexOf(measurements[newMeasurementSelected].imageId);
        if (imageIdIndex !== -1) {
          csViewport.setImageIdIndex(imageIdIndex);
        }
      }
    });
  }, [dataSource, srDisplaySet, activeImageDisplaySetData, viewportId]);
  const getCornerstoneViewport = (0,react__WEBPACK_IMPORTED_MODULE_1__.useCallback)(() => {
    if (!activeImageDisplaySetData) {
      return null;
    }
    const {
      component: Component
    } = extensionManager.getModuleEntry('@ohif/extension-cornerstone.viewportModule.cornerstone');
    const {
      measurements
    } = srDisplaySet;
    const measurement = measurements[measurementSelected];
    if (!measurement) {
      return null;
    }
    const initialImageIndex = activeImageDisplaySetData.images.findIndex(image => image.imageId === measurement.imageId);
    return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(Component, _extends({}, props, {
      // should be passed second since we don't want SR displaySet to
      // override the activeImageDisplaySetData
      displaySets: [activeImageDisplaySetData]
      // It is possible that there is a hanging protocol applying viewportOptions
      // for the SR, so inherit the viewport options
      // TODO: Ensure the viewport options are set correctly with respect to
      // stack etc, in the incoming viewport options.
      ,
      viewportOptions: {
        ...viewportOptions,
        toolGroupId: `${SR_TOOLGROUP_BASE_NAME}`,
        // viewportType should not be required, as the stack type should be
        // required already in order to view SR, but sometimes segmentation
        // views set the viewport type without fixing the allowed display
        viewportType: 'stack',
        // The positionIds for the viewport aren't meaningful for the child display sets
        positionIds: null
      },
      onElementEnabled: onElementEnabled,
      initialImageIndex: initialImageIndex,
      isJumpToMeasurementDisabled: true
    }));
  }, [activeImageDisplaySetData, viewportId, measurementSelected]);
  const onMeasurementChange = (0,react__WEBPACK_IMPORTED_MODULE_1__.useCallback)(direction => {
    let newMeasurementSelected = measurementSelected;
    if (direction === 'right') {
      newMeasurementSelected++;
      if (newMeasurementSelected >= measurementCount) {
        newMeasurementSelected = 0;
      }
    } else {
      newMeasurementSelected--;
      if (newMeasurementSelected < 0) {
        newMeasurementSelected = measurementCount - 1;
      }
    }
    setTrackingIdentifiers(newMeasurementSelected);
    updateViewport(newMeasurementSelected);
  }, [measurementSelected, measurementCount, updateViewport, setTrackingIdentifiers]);

  /**
   Cleanup the SR viewport when the viewport is destroyed
   */
  (0,react__WEBPACK_IMPORTED_MODULE_1__.useEffect)(() => {
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

  /**
   * Loading the measurements from the SR viewport, which goes through the
   * isHydratable check, the outcome for the isHydrated state here is always FALSE
   * since we don't do the hydration here. Todo: can't we just set it as false? why
   * we are changing the state here? isHydrated is always false at this stage, and
   * if it is hydrated we don't even use the SR viewport.
   */
  (0,react__WEBPACK_IMPORTED_MODULE_1__.useEffect)(() => {
    if (!srDisplaySet.isLoaded) {
      srDisplaySet.load();
    }
    const numMeasurements = srDisplaySet.measurements.length;
    setMeasurementCount(numMeasurements);
  }, [srDisplaySet]);

  /**
   * Hook to update the tracking identifiers when the selected measurement changes or
   * the element changes
   */
  (0,react__WEBPACK_IMPORTED_MODULE_1__.useEffect)(() => {
    if (!element || !srDisplaySet.isLoaded) {
      return;
    }
    setTrackingIdentifiers(measurementSelected);
  }, [measurementSelected, element, setTrackingIdentifiers, srDisplaySet]);

  /**
   * Todo: what is this, not sure what it does regarding the react aspect,
   * it is updating a local variable? which is not state.
   */
  let isLocked = trackedMeasurements?.context?.trackedSeries?.length > 0;
  (0,react__WEBPACK_IMPORTED_MODULE_1__.useEffect)(() => {
    isLocked = trackedMeasurements?.context?.trackedSeries?.length > 0;
  }, [trackedMeasurements]);

  /**
   * Data fetching for the SR displaySet, which updates the measurements and
   * also gets the referenced image displaySet that SR is based on.
   */
  (0,react__WEBPACK_IMPORTED_MODULE_1__.useEffect)(() => {
    updateViewport(measurementSelected);
  }, [dataSource, srDisplaySet]);

  // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
  let childrenWithProps = null;
  if (!activeImageDisplaySetData || !referencedDisplaySetMetadata) {
    return null;
  }
  if (children && children.length) {
    childrenWithProps = children.map((child, index) => {
      return child && /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.cloneElement(child, {
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
  } = referencedDisplaySetMetadata;

  // TODO -> disabled double click for now: onDoubleClick={_onDoubleClick}
  return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(react__WEBPACK_IMPORTED_MODULE_1__.Fragment, null, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_5__/* .ViewportActionBar */ .uY, {
    onDoubleClick: evt => {
      evt.stopPropagation();
      evt.preventDefault();
    },
    onArrowsClick: onMeasurementChange,
    getStatusComponent: () => _getStatusComponent({
      srDisplaySet,
      viewportId,
      isTracked: false,
      isRehydratable: srDisplaySet.isRehydratable,
      isLocked,
      sendTrackedMeasurementsEvent
    }),
    studyData: {
      label: viewportLabel,
      useAltStyling: true,
      studyDate: formatDate(StudyDate),
      currentSeries: SeriesNumber,
      seriesDescription: SeriesDescription || '',
      patientInformation: {
        patientName: PatientName ? _ohif_core__WEBPACK_IMPORTED_MODULE_3__["default"].utils.formatPN(PatientName.Alphabetic) : '',
        patientSex: PatientSex || '',
        patientAge: PatientAge || '',
        MRN: PatientID || '',
        thickness: SliceThickness ? `${SliceThickness.toFixed(2)}mm` : '',
        spacing: SpacingBetweenSlices !== undefined ? `${SpacingBetweenSlices.toFixed(2)}mm` : '',
        scanner: ManufacturerModelName || ''
      }
    }
  }), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("div", {
    className: "relative flex h-full w-full flex-row overflow-hidden"
  }, getCornerstoneViewport(), childrenWithProps));
}
OHIFCornerstoneSRViewport.propTypes = {
  displaySets: prop_types__WEBPACK_IMPORTED_MODULE_0___default().arrayOf((prop_types__WEBPACK_IMPORTED_MODULE_0___default().object)),
  viewportId: (prop_types__WEBPACK_IMPORTED_MODULE_0___default().string).isRequired,
  dataSource: (prop_types__WEBPACK_IMPORTED_MODULE_0___default().object),
  children: (prop_types__WEBPACK_IMPORTED_MODULE_0___default().node),
  viewportLabel: (prop_types__WEBPACK_IMPORTED_MODULE_0___default().string),
  customProps: (prop_types__WEBPACK_IMPORTED_MODULE_0___default().object),
  viewportOptions: (prop_types__WEBPACK_IMPORTED_MODULE_0___default().object),
  viewportLabel: (prop_types__WEBPACK_IMPORTED_MODULE_0___default().string),
  servicesManager: prop_types__WEBPACK_IMPORTED_MODULE_0___default().instanceOf(_ohif_core__WEBPACK_IMPORTED_MODULE_3__/* .ServicesManager */ .Xw).isRequired,
  extensionManager: prop_types__WEBPACK_IMPORTED_MODULE_0___default().instanceOf(_ohif_core__WEBPACK_IMPORTED_MODULE_3__/* .ExtensionManager */ .W$).isRequired
};
OHIFCornerstoneSRViewport.defaultProps = {
  customProps: {}
};
async function _getViewportReferencedDisplaySetData(displaySet, measurementSelected, displaySetService) {
  const {
    measurements
  } = displaySet;
  const measurement = measurements[measurementSelected];
  const {
    displaySetInstanceUID
  } = measurement;
  const referencedDisplaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);
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
  return {
    referencedDisplaySetMetadata,
    referencedDisplaySet
  };
}
function _getStatusComponent(_ref4) {
  let {
    srDisplaySet,
    viewportId,
    isRehydratable,
    isLocked,
    sendTrackedMeasurementsEvent
  } = _ref4;
  const handleMouseUp = () => {
    sendTrackedMeasurementsEvent('HYDRATE_SR', {
      displaySetInstanceUID: srDisplaySet.displaySetInstanceUID,
      viewportId
    });
  };
  const {
    t
  } = (0,react_i18next__WEBPACK_IMPORTED_MODULE_2__/* .useTranslation */ .$G)('Common');
  const loadStr = t('LOAD');

  // 1 - Incompatible
  // 2 - Locked
  // 3 - Rehydratable / Open
  const state = isRehydratable && !isLocked ? 3 : isRehydratable && isLocked ? 2 : 1;
  let ToolTipMessage = null;
  let StatusIcon = null;
  switch (state) {
    case 1:
      StatusIcon = () => /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_5__/* .Icon */ .JO, {
        name: "status-alert"
      });
      ToolTipMessage = () => /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("div", null, "This structured report is not compatible", /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("br", null), "with this application.");
      break;
    case 2:
      StatusIcon = () => /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_5__/* .Icon */ .JO, {
        name: "status-locked"
      });
      ToolTipMessage = () => /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("div", null, "This structured report is currently read-only", /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("br", null), "because you are tracking measurements in", /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("br", null), "another viewport.");
      break;
    case 3:
      StatusIcon = () => /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_5__/* .Icon */ .JO, {
        className: "text-aqua-pale",
        name: "status-untracked"
      });
      ToolTipMessage = () => /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("div", null, `Click ${loadStr} to restore measurements.`);
  }
  const StatusArea = () => /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("div", {
    className: "flex h-6 cursor-default text-sm leading-6 text-white"
  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("div", {
    className: "bg-customgray-100 flex min-w-[45px] items-center rounded-l-xl rounded-r p-1"
  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(StatusIcon, null), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("span", {
    className: "ml-1"
  }, "SR")), state === 3 && /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement("div", {
    className: "bg-primary-main hover:bg-primary-light ml-1 cursor-pointer rounded px-1.5 hover:text-black"
    // Using onMouseUp here because onClick is not working when the viewport is not active and is styled with pointer-events:none
    ,
    onMouseUp: handleMouseUp
  }, loadStr));
  return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(react__WEBPACK_IMPORTED_MODULE_1__.Fragment, null, ToolTipMessage && /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_5__/* .Tooltip */ .u, {
    content: /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(ToolTipMessage, null),
    position: "bottom-left"
  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(StatusArea, null)), !ToolTipMessage && /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_1__.createElement(StatusArea, null));
}
/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = (OHIFCornerstoneSRViewport);

/***/ })

}]);