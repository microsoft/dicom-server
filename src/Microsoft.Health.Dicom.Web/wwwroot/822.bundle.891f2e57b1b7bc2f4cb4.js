"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[822],{

/***/ 86822:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (__WEBPACK_DEFAULT_EXPORT__)
/* harmony export */ });
/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(43001);
/* harmony import */ var prop_types__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(3827);
/* harmony import */ var prop_types__WEBPACK_IMPORTED_MODULE_1___default = /*#__PURE__*/__webpack_require__.n(prop_types__WEBPACK_IMPORTED_MODULE_1__);
/* harmony import */ var _ohif_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(71771);
/* harmony import */ var _ohif_ui__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(71783);
/* harmony import */ var react_i18next__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(69190);
/* harmony import */ var _cornerstonejs_tools__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(14957);
/* harmony import */ var _getContextModule__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(41832);
/* harmony import */ var _cornerstonejs_core__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(3743);
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }








const {
  formatDate
} = _ohif_core__WEBPACK_IMPORTED_MODULE_2__.utils;
function TrackedCornerstoneViewport(props) {
  const {
    displaySets,
    viewportId,
    viewportLabel,
    servicesManager,
    extensionManager
  } = props;
  const {
    t
  } = (0,react_i18next__WEBPACK_IMPORTED_MODULE_4__/* .useTranslation */ .$G)('Common');
  const {
    measurementService,
    cornerstoneViewportService,
    viewportGridService
  } = servicesManager.services;

  // Todo: handling more than one displaySet on the same viewport
  const displaySet = displaySets[0];
  const [trackedMeasurements, sendTrackedMeasurementsEvent] = (0,_getContextModule__WEBPACK_IMPORTED_MODULE_6__/* .useTrackedMeasurements */ .I)();
  const [isTracked, setIsTracked] = (0,react__WEBPACK_IMPORTED_MODULE_0__.useState)(false);
  const [trackedMeasurementUID, setTrackedMeasurementUID] = (0,react__WEBPACK_IMPORTED_MODULE_0__.useState)(null);
  const [viewportElem, setViewportElem] = (0,react__WEBPACK_IMPORTED_MODULE_0__.useState)(null);
  const {
    trackedSeries
  } = trackedMeasurements.context;
  const {
    SeriesDate,
    SeriesDescription,
    SeriesInstanceUID,
    SeriesNumber
  } = displaySet;
  const {
    PatientID,
    PatientName,
    PatientSex,
    PatientAge,
    SliceThickness,
    SpacingBetweenSlices,
    StudyDate,
    ManufacturerModelName
  } = displaySet.images[0];
  const updateIsTracked = (0,react__WEBPACK_IMPORTED_MODULE_0__.useCallback)(() => {
    const viewport = cornerstoneViewportService.getCornerstoneViewport(viewportId);
    if (viewport instanceof _cornerstonejs_core__WEBPACK_IMPORTED_MODULE_7__.BaseVolumeViewport) {
      // A current image id will only exist for volume viewports that can have measurements tracked.
      // Typically these are those volume viewports for the series of acquisition.
      const currentImageId = viewport?.getCurrentImageId();
      if (!currentImageId) {
        if (isTracked) {
          setIsTracked(false);
        }
        return;
      }
    }
    if (trackedSeries.includes(SeriesInstanceUID) !== isTracked) {
      setIsTracked(!isTracked);
    }
  }, [isTracked, trackedMeasurements, viewportId, SeriesInstanceUID]);
  const onElementEnabled = (0,react__WEBPACK_IMPORTED_MODULE_0__.useCallback)(evt => {
    if (evt.detail.element !== viewportElem) {
      // The VOLUME_VIEWPORT_NEW_VOLUME event allows updateIsTracked to reliably fetch the image id for a volume viewport.
      evt.detail.element?.addEventListener(_cornerstonejs_core__WEBPACK_IMPORTED_MODULE_7__.Enums.Events.VOLUME_VIEWPORT_NEW_VOLUME, updateIsTracked);
      setViewportElem(evt.detail.element);
    }
  }, [updateIsTracked, viewportElem]);
  const onElementDisabled = (0,react__WEBPACK_IMPORTED_MODULE_0__.useCallback)(() => {
    viewportElem?.removeEventListener(_cornerstonejs_core__WEBPACK_IMPORTED_MODULE_7__.Enums.Events.VOLUME_VIEWPORT_NEW_VOLUME, updateIsTracked);
  }, [updateIsTracked, viewportElem]);
  (0,react__WEBPACK_IMPORTED_MODULE_0__.useEffect)(updateIsTracked, [updateIsTracked]);
  (0,react__WEBPACK_IMPORTED_MODULE_0__.useEffect)(() => {
    const {
      unsubscribe
    } = cornerstoneViewportService.subscribe(cornerstoneViewportService.EVENTS.VIEWPORT_DATA_CHANGED, props => {
      if (props.viewportId !== viewportId) {
        return;
      }
      updateIsTracked();
    });
    return () => {
      unsubscribe();
    };
  }, [updateIsTracked, viewportId]);
  (0,react__WEBPACK_IMPORTED_MODULE_0__.useEffect)(() => {
    if (isTracked) {
      _cornerstonejs_tools__WEBPACK_IMPORTED_MODULE_5__.annotation.config.style.setViewportToolStyles(viewportId, {
        global: {
          lineDash: ''
        }
      });
      cornerstoneViewportService.getRenderingEngine().renderViewport(viewportId);
      return;
    }
    _cornerstonejs_tools__WEBPACK_IMPORTED_MODULE_5__.annotation.config.style.setViewportToolStyles(viewportId, {
      global: {
        lineDash: '4,4'
      }
    });
    cornerstoneViewportService.getRenderingEngine().renderViewport(viewportId);
    return () => {
      _cornerstonejs_tools__WEBPACK_IMPORTED_MODULE_5__.annotation.config.style.setViewportToolStyles(viewportId, {});
    };
  }, [isTracked]);

  /**
   * The effect for listening to measurement service measurement added events
   * and in turn firing an event to update the measurement tracking state machine.
   * The TrackedCornerstoneViewport is the best place for this because when
   * a measurement is added, at least one TrackedCornerstoneViewport will be in
   * the DOM and thus can react to the events fired.
   */
  (0,react__WEBPACK_IMPORTED_MODULE_0__.useEffect)(() => {
    const added = measurementService.EVENTS.MEASUREMENT_ADDED;
    const addedRaw = measurementService.EVENTS.RAW_MEASUREMENT_ADDED;
    const subscriptions = [];
    [added, addedRaw].forEach(evt => {
      subscriptions.push(measurementService.subscribe(evt, _ref => {
        let {
          source,
          measurement
        } = _ref;
        const {
          activeViewportId
        } = viewportGridService.getState();

        // Each TrackedCornerstoneViewport receives the MeasurementService's events.
        // Only send the tracked measurements event for the active viewport to avoid
        // sending it more than once.
        if (viewportId === activeViewportId) {
          const {
            referenceStudyUID: StudyInstanceUID,
            referenceSeriesUID: SeriesInstanceUID
          } = measurement;
          sendTrackedMeasurementsEvent('SET_DIRTY', {
            SeriesInstanceUID
          });
          sendTrackedMeasurementsEvent('TRACK_SERIES', {
            viewportId,
            StudyInstanceUID,
            SeriesInstanceUID
          });
        }
      }).unsubscribe);
    });
    return () => {
      subscriptions.forEach(unsub => {
        unsub();
      });
    };
  }, [measurementService, sendTrackedMeasurementsEvent, viewportId, viewportGridService]);
  function switchMeasurement(direction) {
    const newTrackedMeasurementUID = _getNextMeasurementUID(direction, servicesManager, trackedMeasurementUID, trackedMeasurements);
    if (!newTrackedMeasurementUID) {
      return;
    }
    setTrackedMeasurementUID(newTrackedMeasurementUID);
    measurementService.jumpToMeasurement(viewportId, newTrackedMeasurementUID);
  }
  const getCornerstoneViewport = () => {
    const {
      component: Component
    } = extensionManager.getModuleEntry('@ohif/extension-cornerstone.viewportModule.cornerstone');
    return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(Component, _extends({}, props, {
      onElementEnabled: onElementEnabled,
      onElementDisabled: onElementDisabled
    }));
  };
  return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(react__WEBPACK_IMPORTED_MODULE_0__.Fragment, null, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_3__/* .ViewportActionBar */ .uY, {
    onDoubleClick: evt => {
      evt.stopPropagation();
      evt.preventDefault();
    },
    useAltStyling: isTracked,
    onArrowsClick: direction => switchMeasurement(direction),
    getStatusComponent: () => _getStatusComponent(isTracked),
    studyData: {
      label: viewportLabel,
      studyDate: formatDate(SeriesDate) || formatDate(StudyDate) || t('NoStudyDate'),
      currentSeries: SeriesNumber,
      // TODO - switch entire currentSeries to be UID based or actual position based
      seriesDescription: SeriesDescription,
      patientInformation: {
        patientName: PatientName ? _ohif_core__WEBPACK_IMPORTED_MODULE_2__["default"].utils.formatPN(PatientName) : '',
        patientSex: PatientSex || '',
        patientAge: PatientAge || '',
        MRN: PatientID || '',
        thickness: SliceThickness ? `${parseFloat(SliceThickness).toFixed(2)}` : '',
        thicknessUnits: 'mm',
        spacing: SpacingBetweenSlices !== undefined ? `${parseFloat(SpacingBetweenSlices).toFixed(2)}mm` : '',
        scanner: ManufacturerModelName || ''
      }
    }
  }), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("div", {
    className: "relative flex h-full w-full flex-row overflow-hidden"
  }, getCornerstoneViewport()));
}
TrackedCornerstoneViewport.propTypes = {
  displaySets: prop_types__WEBPACK_IMPORTED_MODULE_1___default().arrayOf((prop_types__WEBPACK_IMPORTED_MODULE_1___default().object).isRequired).isRequired,
  viewportId: (prop_types__WEBPACK_IMPORTED_MODULE_1___default().string).isRequired,
  dataSource: (prop_types__WEBPACK_IMPORTED_MODULE_1___default().object),
  children: (prop_types__WEBPACK_IMPORTED_MODULE_1___default().node),
  customProps: (prop_types__WEBPACK_IMPORTED_MODULE_1___default().object)
};
TrackedCornerstoneViewport.defaultProps = {
  customProps: {}
};
function _getNextMeasurementUID(direction, servicesManager, trackedMeasurementId, trackedMeasurements) {
  const {
    measurementService,
    viewportGridService
  } = servicesManager.services;
  const measurements = measurementService.getMeasurements();
  const {
    activeViewportId,
    viewports
  } = viewportGridService.getState();
  const {
    displaySetInstanceUIDs: activeViewportDisplaySetInstanceUIDs
  } = viewports.get(activeViewportId);
  const {
    trackedSeries
  } = trackedMeasurements.context;

  // Get the potentially trackable measurements for the series of the
  // active viewport.
  // The measurements to jump between are the same
  // regardless if this series is tracked or not.

  const filteredMeasurements = measurements.filter(m => trackedSeries.includes(m.referenceSeriesUID) && activeViewportDisplaySetInstanceUIDs.includes(m.displaySetInstanceUID));
  if (!filteredMeasurements.length) {
    // No measurements on this series.
    return;
  }
  const measurementCount = filteredMeasurements.length;
  const uids = filteredMeasurements.map(fm => fm.uid);
  let measurementIndex = uids.findIndex(uid => uid === trackedMeasurementId);
  if (measurementIndex === -1) {
    // Not tracking a measurement, or previous measurement now deleted, revert to 0.
    measurementIndex = 0;
  } else {
    if (direction === 'left') {
      measurementIndex--;
      if (measurementIndex < 0) {
        measurementIndex = measurementCount - 1;
      }
    } else if (direction === 'right') {
      measurementIndex++;
      if (measurementIndex === measurementCount) {
        measurementIndex = 0;
      }
    }
  }
  const newTrackedMeasurementId = uids[measurementIndex];
  return newTrackedMeasurementId;
}
function _getStatusComponent(isTracked) {
  const trackedIcon = isTracked ? 'status-tracked' : 'status-untracked';
  return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("div", {
    className: "relative"
  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_3__/* .Tooltip */ .u, {
    position: "bottom-left",
    content: /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("div", {
      className: "flex py-2"
    }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("div", {
      className: "flex pt-1"
    }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_3__/* .Icon */ .JO, {
      name: "info-link",
      className: "text-primary-main w-4"
    })), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("div", {
      className: "ml-4 flex"
    }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("span", {
      className: "text-common-light text-base"
    }, isTracked ? /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(react__WEBPACK_IMPORTED_MODULE_0__.Fragment, null, "Series is", /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("span", {
      className: "font-bold text-white"
    }, " tracked"), " and can be viewed ", /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("br", null), ' ', "in the measurement panel") : /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(react__WEBPACK_IMPORTED_MODULE_0__.Fragment, null, "Measurements for", /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("span", {
      className: "font-bold text-white"
    }, " untracked "), "series ", /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("br", null), " will not be shown in the ", /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("br", null), " measurements panel"))))
  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(_ohif_ui__WEBPACK_IMPORTED_MODULE_3__/* .Icon */ .JO, {
    name: trackedIcon,
    className: "text-aqua-pale"
  })));
}
/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = (TrackedCornerstoneViewport);

/***/ })

}]);