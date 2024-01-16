"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[19,579],{

/***/ 41832:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {


// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  Z: () => (/* binding */ src_getContextModule),
  I: () => (/* reexport */ useTrackedMeasurements)
});

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../../../node_modules/xstate/es/index.js + 22 modules
var es = __webpack_require__(261);
// EXTERNAL MODULE: ../../../node_modules/@xstate/react/es/index.js + 8 modules
var react_es = __webpack_require__(44530);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/measurementTrackingMachine.js

const RESPONSE = {
  NO_NEVER: -1,
  CANCEL: 0,
  CREATE_REPORT: 1,
  ADD_SERIES: 2,
  SET_STUDY_AND_SERIES: 3,
  NO_NOT_FOR_SERIES: 4,
  HYDRATE_REPORT: 5
};
const machineConfiguration = {
  id: 'measurementTracking',
  initial: 'idle',
  context: {
    activeViewportId: null,
    trackedStudy: '',
    trackedSeries: [],
    ignoredSeries: [],
    //
    prevTrackedStudy: '',
    prevTrackedSeries: [],
    prevIgnoredSeries: [],
    //
    ignoredSRSeriesForHydration: [],
    isDirty: false
  },
  states: {
    off: {
      type: 'final'
    },
    idle: {
      entry: 'clearContext',
      on: {
        TRACK_SERIES: 'promptBeginTracking',
        // Unused? We may only do PROMPT_HYDRATE_SR now?
        SET_TRACKED_SERIES: [{
          target: 'tracking',
          actions: ['setTrackedStudyAndMultipleSeries', 'setIsDirtyToClean']
        }],
        PROMPT_HYDRATE_SR: {
          target: 'promptHydrateStructuredReport',
          cond: 'hasNotIgnoredSRSeriesForHydration'
        },
        RESTORE_PROMPT_HYDRATE_SR: 'promptHydrateStructuredReport',
        HYDRATE_SR: 'hydrateStructuredReport',
        UPDATE_ACTIVE_VIEWPORT_ID: {
          actions: (0,es/* assign */.f0)({
            activeViewportId: (_, event) => event.activeViewportId
          })
        }
      }
    },
    promptBeginTracking: {
      invoke: {
        src: 'promptBeginTracking',
        onDone: [{
          target: 'tracking',
          actions: ['setTrackedStudyAndSeries', 'setIsDirty'],
          cond: 'shouldSetStudyAndSeries'
        }, {
          target: 'off',
          cond: 'shouldKillMachine'
        }, {
          target: 'idle'
        }],
        onError: {
          target: 'idle'
        }
      }
    },
    tracking: {
      on: {
        TRACK_SERIES: [{
          target: 'promptTrackNewStudy',
          cond: 'isNewStudy'
        }, {
          target: 'promptTrackNewSeries',
          cond: 'isNewSeries'
        }],
        UNTRACK_SERIES: [{
          target: 'tracking',
          actions: ['removeTrackedSeries', 'setIsDirty'],
          cond: 'hasRemainingTrackedSeries'
        }, {
          target: 'idle'
        }],
        SET_TRACKED_SERIES: [{
          target: 'tracking',
          actions: ['setTrackedStudyAndMultipleSeries']
        }],
        SAVE_REPORT: 'promptSaveReport',
        SET_DIRTY: [{
          target: 'tracking',
          actions: ['setIsDirty'],
          cond: 'shouldSetDirty'
        }, {
          target: 'tracking'
        }]
      }
    },
    promptTrackNewSeries: {
      invoke: {
        src: 'promptTrackNewSeries',
        onDone: [{
          target: 'tracking',
          actions: ['addTrackedSeries', 'setIsDirty'],
          cond: 'shouldAddSeries'
        }, {
          target: 'tracking',
          actions: ['discardPreviouslyTrackedMeasurements', 'setTrackedStudyAndSeries', 'setIsDirty'],
          cond: 'shouldSetStudyAndSeries'
        }, {
          target: 'promptSaveReport',
          cond: 'shouldPromptSaveReport'
        }, {
          target: 'tracking'
        }],
        onError: {
          target: 'idle'
        }
      }
    },
    promptTrackNewStudy: {
      invoke: {
        src: 'promptTrackNewStudy',
        onDone: [{
          target: 'tracking',
          actions: ['discardPreviouslyTrackedMeasurements', 'setTrackedStudyAndSeries', 'setIsDirty'],
          cond: 'shouldSetStudyAndSeries'
        }, {
          target: 'tracking',
          actions: ['ignoreSeries'],
          cond: 'shouldAddIgnoredSeries'
        }, {
          target: 'promptSaveReport',
          cond: 'shouldPromptSaveReport'
        }, {
          target: 'tracking'
        }],
        onError: {
          target: 'idle'
        }
      }
    },
    promptSaveReport: {
      invoke: {
        src: 'promptSaveReport',
        onDone: [
        // "clicked the save button"
        // - should clear all measurements
        // - show DICOM SR
        {
          target: 'idle',
          actions: ['clearAllMeasurements', 'showStructuredReportDisplaySetInActiveViewport'],
          cond: 'shouldSaveAndContinueWithSameReport'
        },
        // "starting a new report"
        // - remove "just saved" measurements
        // - start tracking a new study + report
        {
          target: 'tracking',
          actions: ['discardPreviouslyTrackedMeasurements', 'setTrackedStudyAndSeries'],
          cond: 'shouldSaveAndStartNewReport'
        },
        // Cancel, back to tracking
        {
          target: 'tracking'
        }],
        onError: {
          target: 'idle'
        }
      }
    },
    promptHydrateStructuredReport: {
      invoke: {
        src: 'promptHydrateStructuredReport',
        onDone: [{
          target: 'tracking',
          actions: ['setTrackedStudyAndMultipleSeries', 'jumpToFirstMeasurementInActiveViewport', 'setIsDirtyToClean'],
          cond: 'shouldHydrateStructuredReport'
        }, {
          target: 'idle',
          actions: ['ignoreHydrationForSRSeries'],
          cond: 'shouldIgnoreHydrationForSR'
        }],
        onError: {
          target: 'idle'
        }
      }
    },
    hydrateStructuredReport: {
      invoke: {
        src: 'hydrateStructuredReport',
        onDone: [{
          target: 'tracking',
          actions: ['setTrackedStudyAndMultipleSeries', 'jumpToFirstMeasurementInActiveViewport', 'setIsDirtyToClean']
        }],
        onError: {
          target: 'idle'
        }
      }
    }
  },
  strict: true
};
const defaultOptions = {
  services: {
    promptBeginTracking: (ctx, evt) => {
      // return { userResponse, StudyInstanceUID, SeriesInstanceUID }
    },
    promptTrackNewStudy: (ctx, evt) => {
      // return { userResponse, StudyInstanceUID, SeriesInstanceUID }
    },
    promptTrackNewSeries: (ctx, evt) => {
      // return { userResponse, StudyInstanceUID, SeriesInstanceUID }
    }
  },
  actions: {
    discardPreviouslyTrackedMeasurements: (ctx, evt) => {
      console.log('discardPreviouslyTrackedMeasurements: not implemented');
    },
    clearAllMeasurements: (ctx, evt) => {
      console.log('clearAllMeasurements: not implemented');
    },
    jumpToFirstMeasurementInActiveViewport: (ctx, evt) => {
      console.warn('jumpToFirstMeasurementInActiveViewport: not implemented');
    },
    showStructuredReportDisplaySetInActiveViewport: (ctx, evt) => {
      console.warn('showStructuredReportDisplaySetInActiveViewport: not implemented');
    },
    clearContext: (0,es/* assign */.f0)({
      trackedStudy: '',
      trackedSeries: [],
      ignoredSeries: [],
      prevTrackedStudy: '',
      prevTrackedSeries: [],
      prevIgnoredSeries: []
    }),
    // Promise resolves w/ `evt.data.*`
    setTrackedStudyAndSeries: (0,es/* assign */.f0)((ctx, evt) => ({
      prevTrackedStudy: ctx.trackedStudy,
      prevTrackedSeries: ctx.trackedSeries.slice(),
      prevIgnoredSeries: ctx.ignoredSeries.slice(),
      //
      trackedStudy: evt.data.StudyInstanceUID,
      trackedSeries: [evt.data.SeriesInstanceUID],
      ignoredSeries: []
    })),
    setTrackedStudyAndMultipleSeries: (0,es/* assign */.f0)((ctx, evt) => {
      const studyInstanceUID = evt.StudyInstanceUID || evt.data.StudyInstanceUID;
      const seriesInstanceUIDs = evt.SeriesInstanceUIDs || evt.data.SeriesInstanceUIDs;
      return {
        prevTrackedStudy: ctx.trackedStudy,
        prevTrackedSeries: ctx.trackedSeries.slice(),
        prevIgnoredSeries: ctx.ignoredSeries.slice(),
        //
        trackedStudy: studyInstanceUID,
        trackedSeries: [...ctx.trackedSeries, ...seriesInstanceUIDs],
        ignoredSeries: []
      };
    }),
    setIsDirtyToClean: (0,es/* assign */.f0)((ctx, evt) => ({
      isDirty: false
    })),
    setIsDirty: (0,es/* assign */.f0)((ctx, evt) => ({
      isDirty: true
    })),
    ignoreSeries: (0,es/* assign */.f0)((ctx, evt) => ({
      prevIgnoredSeries: [...ctx.ignoredSeries],
      ignoredSeries: [...ctx.ignoredSeries, evt.data.SeriesInstanceUID]
    })),
    ignoreHydrationForSRSeries: (0,es/* assign */.f0)((ctx, evt) => ({
      ignoredSRSeriesForHydration: [...ctx.ignoredSRSeriesForHydration, evt.data.srSeriesInstanceUID]
    })),
    addTrackedSeries: (0,es/* assign */.f0)((ctx, evt) => ({
      prevTrackedSeries: [...ctx.trackedSeries],
      trackedSeries: [...ctx.trackedSeries, evt.data.SeriesInstanceUID]
    })),
    removeTrackedSeries: (0,es/* assign */.f0)((ctx, evt) => ({
      prevTrackedSeries: ctx.trackedSeries.slice().filter(ser => ser !== evt.SeriesInstanceUID),
      trackedSeries: ctx.trackedSeries.slice().filter(ser => ser !== evt.SeriesInstanceUID)
    }))
  },
  guards: {
    // We set dirty any time we performan an action that:
    // - Tracks a new study
    // - Tracks a new series
    // - Adds a measurement to an already tracked study/series
    //
    // We set clean any time we restore from an SR
    //
    // This guard/condition is specific to "new measurements"
    // to make sure we only track dirty when the new measurement is specific
    // to a series we're already tracking
    //
    // tl;dr
    // Any report change, that is not a hydration of an existing report, should
    // result in a "dirty" report
    //
    // Where dirty means there would be "loss of data" if we blew away measurements
    // without creating a new SR.
    shouldSetDirty: (ctx, evt) => {
      return (
        // When would this happen?
        evt.SeriesInstanceUID === undefined || ctx.trackedSeries.includes(evt.SeriesInstanceUID)
      );
    },
    shouldKillMachine: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.NO_NEVER,
    shouldAddSeries: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.ADD_SERIES,
    shouldSetStudyAndSeries: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.SET_STUDY_AND_SERIES,
    shouldAddIgnoredSeries: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.NO_NOT_FOR_SERIES,
    shouldPromptSaveReport: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.CREATE_REPORT,
    shouldIgnoreHydrationForSR: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.CANCEL,
    shouldSaveAndContinueWithSameReport: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.CREATE_REPORT && evt.data.isBackupSave === true,
    shouldSaveAndStartNewReport: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.CREATE_REPORT && evt.data.isBackupSave === false,
    shouldHydrateStructuredReport: (ctx, evt) => evt.data && evt.data.userResponse === RESPONSE.HYDRATE_REPORT,
    // Has more than 1, or SeriesInstanceUID is not in list
    // --> Post removal would have non-empty trackedSeries array
    hasRemainingTrackedSeries: (ctx, evt) => ctx.trackedSeries.length > 1 || !ctx.trackedSeries.includes(evt.SeriesInstanceUID),
    hasNotIgnoredSRSeriesForHydration: (ctx, evt) => {
      return !ctx.ignoredSRSeriesForHydration.includes(evt.SeriesInstanceUID);
    },
    isNewStudy: (ctx, evt) => !ctx.ignoredSeries.includes(evt.SeriesInstanceUID) && ctx.trackedStudy !== evt.StudyInstanceUID,
    isNewSeries: (ctx, evt) => !ctx.ignoredSeries.includes(evt.SeriesInstanceUID) && !ctx.trackedSeries.includes(evt.SeriesInstanceUID)
  }
};

;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/promptBeginTracking.js

const promptBeginTracking_RESPONSE = {
  NO_NEVER: -1,
  CANCEL: 0,
  CREATE_REPORT: 1,
  ADD_SERIES: 2,
  SET_STUDY_AND_SERIES: 3
};
function promptBeginTracking(_ref, ctx, evt) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const {
    uiViewportDialogService
  } = servicesManager.services;
  const {
    viewportId,
    StudyInstanceUID,
    SeriesInstanceUID
  } = evt;
  return new Promise(async function (resolve, reject) {
    let promptResult = await _askTrackMeasurements(uiViewportDialogService, viewportId);
    resolve({
      userResponse: promptResult,
      StudyInstanceUID,
      SeriesInstanceUID,
      viewportId
    });
  });
}
function _askTrackMeasurements(uiViewportDialogService, viewportId) {
  return new Promise(function (resolve, reject) {
    const message = 'Track measurements for this series?';
    const actions = [{
      id: 'prompt-begin-tracking-cancel',
      type: src/* ButtonEnums.type */.LZ.dt.secondary,
      text: 'No',
      value: promptBeginTracking_RESPONSE.CANCEL
    }, {
      id: 'prompt-begin-tracking-no-do-not-ask-again',
      type: src/* ButtonEnums.type */.LZ.dt.secondary,
      text: 'No, do not ask again',
      value: promptBeginTracking_RESPONSE.NO_NEVER
    }, {
      id: 'prompt-begin-tracking-yes',
      type: src/* ButtonEnums.type */.LZ.dt.primary,
      text: 'Yes',
      value: promptBeginTracking_RESPONSE.SET_STUDY_AND_SERIES
    }];
    const onSubmit = result => {
      uiViewportDialogService.hide();
      resolve(result);
    };
    uiViewportDialogService.show({
      viewportId,
      id: 'measurement-tracking-prompt-begin-tracking',
      type: 'info',
      message,
      actions,
      onSubmit,
      onOutsideClick: () => {
        uiViewportDialogService.hide();
        resolve(promptBeginTracking_RESPONSE.CANCEL);
      }
    });
  });
}
/* harmony default export */ const TrackedMeasurementsContext_promptBeginTracking = (promptBeginTracking);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/promptTrackNewSeries.js

const promptTrackNewSeries_RESPONSE = {
  NO_NEVER: -1,
  CANCEL: 0,
  CREATE_REPORT: 1,
  ADD_SERIES: 2,
  SET_STUDY_AND_SERIES: 3,
  NO_NOT_FOR_SERIES: 4
};
function promptTrackNewSeries(_ref, ctx, evt) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const {
    UIViewportDialogService
  } = servicesManager.services;
  const {
    viewportId,
    StudyInstanceUID,
    SeriesInstanceUID
  } = evt;
  return new Promise(async function (resolve, reject) {
    let promptResult = await _askShouldAddMeasurements(UIViewportDialogService, viewportId);
    if (promptResult === promptTrackNewSeries_RESPONSE.CREATE_REPORT) {
      promptResult = ctx.isDirty ? await _askSaveDiscardOrCancel(UIViewportDialogService, viewportId) : promptTrackNewSeries_RESPONSE.SET_STUDY_AND_SERIES;
    }
    resolve({
      userResponse: promptResult,
      StudyInstanceUID,
      SeriesInstanceUID,
      viewportId,
      isBackupSave: false
    });
  });
}
function _askShouldAddMeasurements(uiViewportDialogService, viewportId) {
  return new Promise(function (resolve, reject) {
    const message = 'Do you want to add this measurement to the existing report?';
    const actions = [{
      type: src/* ButtonEnums.type */.LZ.dt.secondary,
      text: 'Cancel',
      value: promptTrackNewSeries_RESPONSE.CANCEL
    }, {
      type: src/* ButtonEnums.type */.LZ.dt.primary,
      text: 'Create new report',
      value: promptTrackNewSeries_RESPONSE.CREATE_REPORT
    }, {
      type: src/* ButtonEnums.type */.LZ.dt.primary,
      text: 'Add to existing report',
      value: promptTrackNewSeries_RESPONSE.ADD_SERIES
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
        resolve(promptTrackNewSeries_RESPONSE.CANCEL);
      }
    });
  });
}
function _askSaveDiscardOrCancel(UIViewportDialogService, viewportId) {
  return new Promise(function (resolve, reject) {
    const message = 'You have existing tracked measurements. What would you like to do with your existing tracked measurements?';
    const actions = [{
      type: 'cancel',
      text: 'Cancel',
      value: promptTrackNewSeries_RESPONSE.CANCEL
    }, {
      type: 'secondary',
      text: 'Save',
      value: promptTrackNewSeries_RESPONSE.CREATE_REPORT
    }, {
      type: 'primary',
      text: 'Discard',
      value: promptTrackNewSeries_RESPONSE.SET_STUDY_AND_SERIES
    }];
    const onSubmit = result => {
      UIViewportDialogService.hide();
      resolve(result);
    };
    UIViewportDialogService.show({
      viewportId,
      type: 'warning',
      message,
      actions,
      onSubmit,
      onOutsideClick: () => {
        UIViewportDialogService.hide();
        resolve(promptTrackNewSeries_RESPONSE.CANCEL);
      }
    });
  });
}
/* harmony default export */ const TrackedMeasurementsContext_promptTrackNewSeries = (promptTrackNewSeries);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/promptTrackNewStudy.js
const promptTrackNewStudy_RESPONSE = {
  NO_NEVER: -1,
  CANCEL: 0,
  CREATE_REPORT: 1,
  ADD_SERIES: 2,
  SET_STUDY_AND_SERIES: 3,
  NO_NOT_FOR_SERIES: 4
};
function promptTrackNewStudy(_ref, ctx, evt) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const {
    UIViewportDialogService
  } = servicesManager.services;
  const {
    viewportId,
    StudyInstanceUID,
    SeriesInstanceUID
  } = evt;
  return new Promise(async function (resolve, reject) {
    let promptResult = await promptTrackNewStudy_askTrackMeasurements(UIViewportDialogService, viewportId);
    if (promptResult === promptTrackNewStudy_RESPONSE.SET_STUDY_AND_SERIES) {
      promptResult = ctx.isDirty ? await promptTrackNewStudy_askSaveDiscardOrCancel(UIViewportDialogService, viewportId) : promptTrackNewStudy_RESPONSE.SET_STUDY_AND_SERIES;
    }
    resolve({
      userResponse: promptResult,
      StudyInstanceUID,
      SeriesInstanceUID,
      viewportId,
      isBackupSave: false
    });
  });
}
function promptTrackNewStudy_askTrackMeasurements(UIViewportDialogService, viewportId) {
  return new Promise(function (resolve, reject) {
    const message = 'Track measurements for this series?';
    const actions = [{
      type: 'cancel',
      text: 'No',
      value: promptTrackNewStudy_RESPONSE.CANCEL
    }, {
      type: 'secondary',
      text: 'No, do not ask again for this series',
      value: promptTrackNewStudy_RESPONSE.NO_NOT_FOR_SERIES
    }, {
      type: 'primary',
      text: 'Yes',
      value: promptTrackNewStudy_RESPONSE.SET_STUDY_AND_SERIES
    }];
    const onSubmit = result => {
      UIViewportDialogService.hide();
      resolve(result);
    };
    UIViewportDialogService.show({
      viewportId,
      type: 'info',
      message,
      actions,
      onSubmit,
      onOutsideClick: () => {
        UIViewportDialogService.hide();
        resolve(promptTrackNewStudy_RESPONSE.CANCEL);
      }
    });
  });
}
function promptTrackNewStudy_askSaveDiscardOrCancel(UIViewportDialogService, viewportId) {
  return new Promise(function (resolve, reject) {
    const message = 'Measurements cannot span across multiple studies. Do you want to save your tracked measurements?';
    const actions = [{
      type: 'cancel',
      text: 'Cancel',
      value: promptTrackNewStudy_RESPONSE.CANCEL
    }, {
      type: 'secondary',
      text: 'No, discard previously tracked series & measurements',
      value: promptTrackNewStudy_RESPONSE.SET_STUDY_AND_SERIES
    }, {
      type: 'primary',
      text: 'Yes',
      value: promptTrackNewStudy_RESPONSE.CREATE_REPORT
    }];
    const onSubmit = result => {
      UIViewportDialogService.hide();
      resolve(result);
    };
    UIViewportDialogService.show({
      viewportId,
      type: 'warning',
      message,
      actions,
      onSubmit,
      onOutsideClick: () => {
        UIViewportDialogService.hide();
        resolve(promptTrackNewStudy_RESPONSE.CANCEL);
      }
    });
  });
}
/* harmony default export */ const TrackedMeasurementsContext_promptTrackNewStudy = (promptTrackNewStudy);
// EXTERNAL MODULE: ../../../extensions/default/src/index.ts + 76 modules
var default_src = __webpack_require__(56342);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/_shared/getNextSRSeriesNumber.js
const MIN_SR_SERIES_NUMBER = 4700;
function getNextSRSeriesNumber(displaySetService) {
  const activeDisplaySets = displaySetService.getActiveDisplaySets();
  const srDisplaySets = activeDisplaySets.filter(ds => ds.Modality === 'SR');
  const srSeriesNumbers = srDisplaySets.map(ds => ds.SeriesNumber);
  const maxSeriesNumber = Math.max(...srSeriesNumbers, MIN_SR_SERIES_NUMBER);
  return maxSeriesNumber + 1;
}
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/_shared/PROMPT_RESPONSES.js
const PROMPT_RESPONSES_RESPONSE = {
  NO_NEVER: -1,
  CANCEL: 0,
  CREATE_REPORT: 1,
  ADD_SERIES: 2,
  SET_STUDY_AND_SERIES: 3,
  NO_NOT_FOR_SERIES: 4
};
/* harmony default export */ const PROMPT_RESPONSES = (PROMPT_RESPONSES_RESPONSE);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/promptSaveReport.js



function promptSaveReport(_ref, ctx, evt) {
  let {
    servicesManager,
    commandsManager,
    extensionManager
  } = _ref;
  const {
    uiDialogService,
    measurementService,
    displaySetService
  } = servicesManager.services;
  const viewportId = evt.viewportId === undefined ? evt.data.viewportId : evt.viewportId;
  const isBackupSave = evt.isBackupSave === undefined ? evt.data.isBackupSave : evt.isBackupSave;
  const StudyInstanceUID = evt?.data?.StudyInstanceUID;
  const SeriesInstanceUID = evt?.data?.SeriesInstanceUID;
  const {
    trackedStudy,
    trackedSeries
  } = ctx;
  let displaySetInstanceUIDs;
  return new Promise(async function (resolve, reject) {
    // TODO: Fallback if (uiDialogService) {
    const promptResult = await (0,default_src.createReportDialogPrompt)(uiDialogService, {
      extensionManager
    });
    if (promptResult.action === PROMPT_RESPONSES.CREATE_REPORT) {
      const dataSources = extensionManager.getDataSources();
      const dataSource = dataSources[0];
      const measurements = measurementService.getMeasurements();
      const trackedMeasurements = measurements.filter(m => trackedStudy === m.referenceStudyUID && trackedSeries.includes(m.referenceSeriesUID));
      const SeriesDescription =
      // isUndefinedOrEmpty
      promptResult.value === undefined || promptResult.value === '' ? 'Research Derived Series' // default
      : promptResult.value; // provided value

      const SeriesNumber = getNextSRSeriesNumber(displaySetService);
      const getReport = async () => {
        return commandsManager.runCommand('storeMeasurements', {
          measurementData: trackedMeasurements,
          dataSource,
          additionalFindingTypes: ['ArrowAnnotate'],
          options: {
            SeriesDescription,
            SeriesNumber
          }
        }, 'CORNERSTONE_STRUCTURED_REPORT');
      };
      displaySetInstanceUIDs = await (0,default_src.createReportAsync)({
        servicesManager,
        getReport
      });
    } else if (promptResult.action === PROMPT_RESPONSES.CANCEL) {
      // Do nothing
    }
    resolve({
      userResponse: promptResult.action,
      createdDisplaySetInstanceUIDs: displaySetInstanceUIDs,
      StudyInstanceUID,
      SeriesInstanceUID,
      viewportId,
      isBackupSave
    });
  });
}
/* harmony default export */ const TrackedMeasurementsContext_promptSaveReport = (promptSaveReport);
// EXTERNAL MODULE: ../../../extensions/cornerstone-dicom-sr/src/index.tsx + 15 modules
var cornerstone_dicom_sr_src = __webpack_require__(42170);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/promptHydrateStructuredReport.js


const promptHydrateStructuredReport_RESPONSE = {
  NO_NEVER: -1,
  CANCEL: 0,
  CREATE_REPORT: 1,
  ADD_SERIES: 2,
  SET_STUDY_AND_SERIES: 3,
  NO_NOT_FOR_SERIES: 4,
  HYDRATE_REPORT: 5
};
function promptHydrateStructuredReport(_ref, ctx, evt) {
  let {
    servicesManager,
    extensionManager,
    appConfig
  } = _ref;
  const {
    uiViewportDialogService,
    displaySetService
  } = servicesManager.services;
  const {
    viewportId,
    displaySetInstanceUID
  } = evt;
  const srDisplaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);
  return new Promise(async function (resolve, reject) {
    const promptResult = await promptHydrateStructuredReport_askTrackMeasurements(uiViewportDialogService, viewportId);

    // Need to do action here... So we can set state...
    let StudyInstanceUID, SeriesInstanceUIDs;
    if (promptResult === promptHydrateStructuredReport_RESPONSE.HYDRATE_REPORT) {
      console.warn('!! HYDRATING STRUCTURED REPORT');
      const hydrationResult = (0,cornerstone_dicom_sr_src.hydrateStructuredReport)({
        servicesManager,
        extensionManager,
        appConfig
      }, displaySetInstanceUID);
      StudyInstanceUID = hydrationResult.StudyInstanceUID;
      SeriesInstanceUIDs = hydrationResult.SeriesInstanceUIDs;
    }
    resolve({
      userResponse: promptResult,
      displaySetInstanceUID: evt.displaySetInstanceUID,
      srSeriesInstanceUID: srDisplaySet.SeriesInstanceUID,
      viewportId,
      StudyInstanceUID,
      SeriesInstanceUIDs
    });
  });
}
function promptHydrateStructuredReport_askTrackMeasurements(uiViewportDialogService, viewportId) {
  return new Promise(function (resolve, reject) {
    const message = 'Do you want to continue tracking measurements for this study?';
    const actions = [{
      type: src/* ButtonEnums.type */.LZ.dt.secondary,
      text: 'No',
      value: promptHydrateStructuredReport_RESPONSE.CANCEL
    }, {
      type: src/* ButtonEnums.type */.LZ.dt.primary,
      text: 'Yes',
      value: promptHydrateStructuredReport_RESPONSE.HYDRATE_REPORT
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
        resolve(promptHydrateStructuredReport_RESPONSE.CANCEL);
      }
    });
  });
}
/* harmony default export */ const TrackedMeasurementsContext_promptHydrateStructuredReport = (promptHydrateStructuredReport);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/hydrateStructuredReport.tsx

function hydrateStructuredReport(_ref, ctx, evt) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const {
    displaySetService
  } = servicesManager.services;
  const {
    viewportId,
    displaySetInstanceUID
  } = evt;
  const srDisplaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);
  return new Promise((resolve, reject) => {
    const hydrationResult = (0,cornerstone_dicom_sr_src.hydrateStructuredReport)({
      servicesManager,
      extensionManager
    }, displaySetInstanceUID);
    const StudyInstanceUID = hydrationResult.StudyInstanceUID;
    const SeriesInstanceUIDs = hydrationResult.SeriesInstanceUIDs;
    resolve({
      displaySetInstanceUID: evt.displaySetInstanceUID,
      srSeriesInstanceUID: srDisplaySet.SeriesInstanceUID,
      viewportId,
      StudyInstanceUID,
      SeriesInstanceUIDs
    });
  });
}
/* harmony default export */ const TrackedMeasurementsContext_hydrateStructuredReport = (hydrateStructuredReport);
// EXTERNAL MODULE: ./state/index.js + 1 modules
var state = __webpack_require__(62657);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/TrackedMeasurementsContext.tsx













const TrackedMeasurementsContext = /*#__PURE__*/react.createContext();
TrackedMeasurementsContext.displayName = 'TrackedMeasurementsContext';
const useTrackedMeasurements = () => (0,react.useContext)(TrackedMeasurementsContext);
const SR_SOPCLASSHANDLERID = '@ohif/extension-cornerstone-dicom-sr.sopClassHandlerModule.dicom-sr';

/**
 *
 * @param {*} param0
 */
function TrackedMeasurementsContextProvider(_ref, // Bound by consumer
_ref2 // Component props
) {
  let {
    servicesManager,
    commandsManager,
    extensionManager
  } = _ref;
  let {
    children
  } = _ref2;
  const [appConfig] = (0,state/* useAppConfig */.M)();
  const [viewportGrid, viewportGridService] = (0,src/* useViewportGrid */.O_)();
  const {
    activeViewportId,
    viewports
  } = viewportGrid;
  const {
    measurementService,
    displaySetService
  } = servicesManager.services;
  const machineOptions = Object.assign({}, defaultOptions);
  machineOptions.actions = Object.assign({}, machineOptions.actions, {
    jumpToFirstMeasurementInActiveViewport: (ctx, evt) => {
      const {
        trackedStudy,
        trackedSeries,
        activeViewportId
      } = ctx;
      const measurements = measurementService.getMeasurements();
      const trackedMeasurements = measurements.filter(m => trackedStudy === m.referenceStudyUID && trackedSeries.includes(m.referenceSeriesUID));
      console.log('jumping to measurement reset viewport', activeViewportId, trackedMeasurements[0]);
      const referencedDisplaySetUID = trackedMeasurements[0].displaySetInstanceUID;
      const referencedDisplaySet = displaySetService.getDisplaySetByUID(referencedDisplaySetUID);
      const referencedImages = referencedDisplaySet.images;
      const isVolumeIdReferenced = referencedImages[0].imageId.startsWith('volumeId');
      const measurementData = trackedMeasurements[0].data;
      let imageIndex = 0;
      if (!isVolumeIdReferenced && measurementData) {
        // if it is imageId referenced find the index of the imageId, we don't have
        // support for volumeId referenced images yet
        imageIndex = referencedImages.findIndex(image => {
          const imageIdToUse = Object.keys(measurementData)[0].substring(8);
          return image.imageId === imageIdToUse;
        });
        if (imageIndex === -1) {
          console.warn('Could not find image index for tracked measurement, using 0');
          imageIndex = 0;
        }
      }
      viewportGridService.setDisplaySetsForViewport({
        viewportId: activeViewportId,
        displaySetInstanceUIDs: [referencedDisplaySetUID],
        viewportOptions: {
          initialImageOptions: {
            index: imageIndex
          }
        }
      });
    },
    showStructuredReportDisplaySetInActiveViewport: (ctx, evt) => {
      if (evt.data.createdDisplaySetInstanceUIDs.length > 0) {
        const StructuredReportDisplaySetInstanceUID = evt.data.createdDisplaySetInstanceUIDs[0];
        viewportGridService.setDisplaySetsForViewport({
          viewportId: evt.data.viewportId,
          displaySetInstanceUIDs: [StructuredReportDisplaySetInstanceUID]
        });
      }
    },
    discardPreviouslyTrackedMeasurements: (ctx, evt) => {
      const measurements = measurementService.getMeasurements();
      const filteredMeasurements = measurements.filter(ms => ctx.prevTrackedSeries.includes(ms.referenceSeriesUID));
      const measurementIds = filteredMeasurements.map(fm => fm.id);
      for (let i = 0; i < measurementIds.length; i++) {
        measurementService.remove(measurementIds[i]);
      }
    },
    clearAllMeasurements: (ctx, evt) => {
      const measurements = measurementService.getMeasurements();
      const measurementIds = measurements.map(fm => fm.uid);
      for (let i = 0; i < measurementIds.length; i++) {
        measurementService.remove(measurementIds[i]);
      }
    }
  });
  machineOptions.services = Object.assign({}, machineOptions.services, {
    promptBeginTracking: TrackedMeasurementsContext_promptBeginTracking.bind(null, {
      servicesManager,
      extensionManager,
      appConfig
    }),
    promptTrackNewSeries: TrackedMeasurementsContext_promptTrackNewSeries.bind(null, {
      servicesManager,
      extensionManager,
      appConfig
    }),
    promptTrackNewStudy: TrackedMeasurementsContext_promptTrackNewStudy.bind(null, {
      servicesManager,
      extensionManager,
      appConfig
    }),
    promptSaveReport: TrackedMeasurementsContext_promptSaveReport.bind(null, {
      servicesManager,
      commandsManager,
      extensionManager,
      appConfig
    }),
    promptHydrateStructuredReport: TrackedMeasurementsContext_promptHydrateStructuredReport.bind(null, {
      servicesManager,
      extensionManager,
      appConfig
    }),
    hydrateStructuredReport: TrackedMeasurementsContext_hydrateStructuredReport.bind(null, {
      servicesManager,
      extensionManager,
      appConfig
    })
  });

  // TODO: IMPROVE
  // - Add measurement_updated to cornerstone; debounced? (ext side, or consumption?)
  // - Friendlier transition/api in front of measurementTracking machine?
  // - Blocked: viewport overlay shouldn't clip when resized
  // TODO: PRIORITY
  // - Fix "ellipses" series description dynamic truncate length
  // - Fix viewport border resize
  // - created/destroyed hooks for extensions (cornerstone measurement subscriptions in it's `init`)

  const measurementTrackingMachine = (0,es/* Machine */.J)(machineConfiguration, machineOptions);
  const [trackedMeasurements, sendTrackedMeasurementsEvent] = (0,react_es/* useMachine */.eO)(measurementTrackingMachine);
  (0,react.useEffect)(() => {
    // Update the state machine with the active viewport ID
    sendTrackedMeasurementsEvent('UPDATE_ACTIVE_VIEWPORT_ID', {
      activeViewportId
    });
  }, [activeViewportId, sendTrackedMeasurementsEvent]);

  // ~~ Listen for changes to ViewportGrid for potential SRs hung in panes when idle
  (0,react.useEffect)(() => {
    if (viewports.size > 0) {
      const activeViewport = viewports.get(activeViewportId);
      if (!activeViewport || !activeViewport?.displaySetInstanceUIDs?.length) {
        return;
      }

      // Todo: Getting the first displaySetInstanceUID is wrong, but we don't have
      // tracking fusion viewports yet. This should change when we do.
      const {
        displaySetService
      } = servicesManager.services;
      const displaySet = displaySetService.getDisplaySetByUID(activeViewport.displaySetInstanceUIDs[0]);
      if (!displaySet) {
        return;
      }

      // If this is an SR produced by our SR SOPClassHandler,
      // and it hasn't been loaded yet, do that now so we
      // can check if it can be rehydrated or not.
      //
      // Note: This happens:
      // - If the viewport is not currently an OHIFCornerstoneSRViewport
      // - If the displaySet has never been hung
      //
      // Otherwise, the displaySet will be loaded by the useEffect handler
      // listening to displaySet changes inside OHIFCornerstoneSRViewport.
      // The issue here is that this handler in TrackedMeasurementsContext
      // ends up occurring before the Viewport is created, so the displaySet
      // is not loaded yet, and isRehydratable is undefined unless we call load().
      if (displaySet.SOPClassHandlerId === SR_SOPCLASSHANDLERID && !displaySet.isLoaded && displaySet.load) {
        displaySet.load();
      }

      // Magic string
      // load function added by our sopClassHandler module
      if (displaySet.SOPClassHandlerId === SR_SOPCLASSHANDLERID && displaySet.isRehydratable === true) {
        console.log('sending event...', trackedMeasurements);
        sendTrackedMeasurementsEvent('PROMPT_HYDRATE_SR', {
          displaySetInstanceUID: displaySet.displaySetInstanceUID,
          SeriesInstanceUID: displaySet.SeriesInstanceUID,
          viewportId: activeViewportId
        });
      }
    }
  }, [activeViewportId, sendTrackedMeasurementsEvent, servicesManager.services, viewports]);
  return /*#__PURE__*/react.createElement(TrackedMeasurementsContext.Provider, {
    value: [trackedMeasurements, sendTrackedMeasurementsEvent]
  }, children);
}
TrackedMeasurementsContextProvider.propTypes = {
  children: prop_types_default().oneOf([(prop_types_default()).func, (prop_types_default()).node]),
  servicesManager: (prop_types_default()).object.isRequired,
  commandsManager: (prop_types_default()).object.isRequired,
  extensionManager: (prop_types_default()).object.isRequired,
  appConfig: (prop_types_default()).object
};

;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/TrackedMeasurementsContext/index.js

;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/contexts/index.js

;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/getContextModule.tsx

function getContextModule(_ref) {
  let {
    servicesManager,
    extensionManager,
    commandsManager
  } = _ref;
  const BoundTrackedMeasurementsContextProvider = TrackedMeasurementsContextProvider.bind(null, {
    servicesManager,
    extensionManager,
    commandsManager
  });
  return [{
    name: 'TrackedMeasurementsContext',
    context: TrackedMeasurementsContext,
    provider: BoundTrackedMeasurementsContextProvider
  }];
}

/* harmony default export */ const src_getContextModule = (getContextModule);

/***/ }),

/***/ 28030:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ measurement_tracking_src)
});

// EXTERNAL MODULE: ../../../extensions/measurement-tracking/src/getContextModule.tsx + 12 modules
var getContextModule = __webpack_require__(41832);
// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../node_modules/react-router-dom/dist/index.js
var dist = __webpack_require__(62474);
// EXTERNAL MODULE: ../../../node_modules/react-i18next/dist/es/index.js + 15 modules
var es = __webpack_require__(69190);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/panels/PanelStudyBrowserTracking/PanelStudyBrowserTracking.tsx







const {
  formatDate
} = src.utils;

/**
 *
 * @param {*} param0
 */
function PanelStudyBrowserTracking(_ref) {
  let {
    servicesManager,
    getImageSrc,
    getStudiesForPatientByMRN,
    requestDisplaySetCreationForStudy,
    dataSource
  } = _ref;
  const {
    displaySetService,
    uiDialogService,
    hangingProtocolService,
    uiNotificationService
  } = servicesManager.services;
  const navigate = (0,dist/* useNavigate */.s0)();
  const {
    t
  } = (0,es/* useTranslation */.$G)('Common');

  // Normally you nest the components so the tree isn't so deep, and the data
  // doesn't have to have such an intense shape. This works well enough for now.
  // Tabs --> Studies --> DisplaySets --> Thumbnails
  const {
    StudyInstanceUIDs
  } = (0,ui_src/* useImageViewer */.zG)();
  const [{
    activeViewportId,
    viewports
  }, viewportGridService] = (0,ui_src/* useViewportGrid */.O_)();
  const [trackedMeasurements, sendTrackedMeasurementsEvent] = (0,getContextModule/* useTrackedMeasurements */.I)();
  const [activeTabName, setActiveTabName] = (0,react.useState)('primary');
  const [expandedStudyInstanceUIDs, setExpandedStudyInstanceUIDs] = (0,react.useState)([...StudyInstanceUIDs]);
  const [studyDisplayList, setStudyDisplayList] = (0,react.useState)([]);
  const [displaySets, setDisplaySets] = (0,react.useState)([]);
  const [thumbnailImageSrcMap, setThumbnailImageSrcMap] = (0,react.useState)({});
  const [jumpToDisplaySet, setJumpToDisplaySet] = (0,react.useState)(null);
  const onDoubleClickThumbnailHandler = displaySetInstanceUID => {
    let updatedViewports = [];
    const viewportId = activeViewportId;
    try {
      updatedViewports = hangingProtocolService.getViewportsRequireUpdate(viewportId, displaySetInstanceUID);
    } catch (error) {
      console.warn(error);
      uiNotificationService.show({
        title: 'Thumbnail Double Click',
        message: 'The selected display sets could not be added to the viewport due to a mismatch in the Hanging Protocol rules.',
        type: 'info',
        duration: 3000
      });
    }
    viewportGridService.setDisplaySetsForViewports(updatedViewports);
  };
  const activeViewportDisplaySetInstanceUIDs = viewports.get(activeViewportId)?.displaySetInstanceUIDs;
  const {
    trackedSeries
  } = trackedMeasurements.context;

  // ~~ studyDisplayList
  (0,react.useEffect)(() => {
    // Fetch all studies for the patient in each primary study
    async function fetchStudiesForPatient(StudyInstanceUID) {
      // current study qido
      const qidoForStudyUID = await dataSource.query.studies.search({
        studyInstanceUid: StudyInstanceUID
      });
      if (!qidoForStudyUID?.length) {
        navigate('/notfoundstudy', '_self');
        throw new Error('Invalid study URL');
      }
      let qidoStudiesForPatient = qidoForStudyUID;

      // try to fetch the prior studies based on the patientID if the
      // server can respond.
      try {
        qidoStudiesForPatient = await getStudiesForPatientByMRN(qidoForStudyUID);
      } catch (error) {
        console.warn(error);
      }
      const mappedStudies = _mapDataSourceStudies(qidoStudiesForPatient);
      const actuallyMappedStudies = mappedStudies.map(qidoStudy => {
        return {
          studyInstanceUid: qidoStudy.StudyInstanceUID,
          date: formatDate(qidoStudy.StudyDate) || t('NoStudyDate'),
          description: qidoStudy.StudyDescription,
          modalities: qidoStudy.ModalitiesInStudy,
          numInstances: qidoStudy.NumInstances
        };
      });
      setStudyDisplayList(prevArray => {
        const ret = [...prevArray];
        for (const study of actuallyMappedStudies) {
          if (!prevArray.find(it => it.studyInstanceUid === study.studyInstanceUid)) {
            ret.push(study);
          }
        }
        return ret;
      });
    }
    StudyInstanceUIDs.forEach(sid => fetchStudiesForPatient(sid));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [StudyInstanceUIDs, getStudiesForPatientByMRN]);

  // ~~ Initial Thumbnails
  (0,react.useEffect)(() => {
    const currentDisplaySets = displaySetService.activeDisplaySets;
    if (!currentDisplaySets.length) {
      return;
    }
    currentDisplaySets.forEach(async dSet => {
      const newImageSrcEntry = {};
      const displaySet = displaySetService.getDisplaySetByUID(dSet.displaySetInstanceUID);
      const imageIds = dataSource.getImageIdsForDisplaySet(displaySet);
      const imageId = imageIds[Math.floor(imageIds.length / 2)];

      // TODO: Is it okay that imageIds are not returned here for SR displaySets?
      if (!imageId || displaySet?.unsupported) {
        return;
      }
      // When the image arrives, render it and store the result in the thumbnailImgSrcMap
      newImageSrcEntry[dSet.displaySetInstanceUID] = await getImageSrc(imageId);
      setThumbnailImageSrcMap(prevState => {
        return {
          ...prevState,
          ...newImageSrcEntry
        };
      });
    });
  }, [displaySetService, dataSource, getImageSrc]);

  // ~~ displaySets
  (0,react.useEffect)(() => {
    const currentDisplaySets = displaySetService.activeDisplaySets;
    if (!currentDisplaySets.length) {
      return;
    }
    const mappedDisplaySets = _mapDisplaySets(currentDisplaySets, thumbnailImageSrcMap, trackedSeries, viewports, viewportGridService, dataSource, displaySetService, uiDialogService, uiNotificationService);
    setDisplaySets(mappedDisplaySets);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [displaySetService.activeDisplaySets, trackedSeries, viewports, dataSource, thumbnailImageSrcMap]);

  // ~~ subscriptions --> displaySets
  (0,react.useEffect)(() => {
    // DISPLAY_SETS_ADDED returns an array of DisplaySets that were added
    const SubscriptionDisplaySetsAdded = displaySetService.subscribe(displaySetService.EVENTS.DISPLAY_SETS_ADDED, data => {
      const {
        displaySetsAdded,
        options
      } = data;
      displaySetsAdded.forEach(async dSet => {
        const displaySetInstanceUID = dSet.displaySetInstanceUID;
        const newImageSrcEntry = {};
        const displaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);
        if (displaySet?.unsupported) {
          return;
        }
        if (options.madeInClient) {
          setJumpToDisplaySet(displaySetInstanceUID);
        }
        const imageIds = dataSource.getImageIdsForDisplaySet(displaySet);
        const imageId = imageIds[Math.floor(imageIds.length / 2)];

        // TODO: Is it okay that imageIds are not returned here for SR displaysets?
        if (!imageId) {
          return;
        }

        // When the image arrives, render it and store the result in the thumbnailImgSrcMap
        newImageSrcEntry[displaySetInstanceUID] = await getImageSrc(imageId);
        setThumbnailImageSrcMap(prevState => {
          return {
            ...prevState,
            ...newImageSrcEntry
          };
        });
      });
    });
    return () => {
      SubscriptionDisplaySetsAdded.unsubscribe();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [displaySetService, dataSource, getImageSrc, thumbnailImageSrcMap, trackedSeries, viewports]);
  (0,react.useEffect)(() => {
    // TODO: Will this always hold _all_ the displaySets we care about?
    // DISPLAY_SETS_CHANGED returns `DisplaySerService.activeDisplaySets`
    const SubscriptionDisplaySetsChanged = displaySetService.subscribe(displaySetService.EVENTS.DISPLAY_SETS_CHANGED, changedDisplaySets => {
      const mappedDisplaySets = _mapDisplaySets(changedDisplaySets, thumbnailImageSrcMap, trackedSeries, viewports, viewportGridService, dataSource, displaySetService, uiDialogService, uiNotificationService);
      setDisplaySets(mappedDisplaySets);
    });
    const SubscriptionDisplaySetMetaDataInvalidated = displaySetService.subscribe(displaySetService.EVENTS.DISPLAY_SET_SERIES_METADATA_INVALIDATED, () => {
      const mappedDisplaySets = _mapDisplaySets(displaySetService.getActiveDisplaySets(), thumbnailImageSrcMap, trackedSeries, viewports, viewportGridService, dataSource, displaySetService, uiDialogService, uiNotificationService);
      setDisplaySets(mappedDisplaySets);
    });
    return () => {
      SubscriptionDisplaySetsChanged.unsubscribe();
      SubscriptionDisplaySetMetaDataInvalidated.unsubscribe();
    };
  }, [thumbnailImageSrcMap, trackedSeries, viewports, dataSource, displaySetService]);
  const tabs = _createStudyBrowserTabs(StudyInstanceUIDs, studyDisplayList, displaySets, hangingProtocolService);

  // TODO: Should not fire this on "close"
  function _handleStudyClick(StudyInstanceUID) {
    const shouldCollapseStudy = expandedStudyInstanceUIDs.includes(StudyInstanceUID);
    const updatedExpandedStudyInstanceUIDs = shouldCollapseStudy ? [...expandedStudyInstanceUIDs.filter(stdyUid => stdyUid !== StudyInstanceUID)] : [...expandedStudyInstanceUIDs, StudyInstanceUID];
    setExpandedStudyInstanceUIDs(updatedExpandedStudyInstanceUIDs);
    if (!shouldCollapseStudy) {
      const madeInClient = true;
      requestDisplaySetCreationForStudy(displaySetService, StudyInstanceUID, madeInClient);
    }
  }
  (0,react.useEffect)(() => {
    if (jumpToDisplaySet) {
      // Get element by displaySetInstanceUID
      const displaySetInstanceUID = jumpToDisplaySet;
      const element = document.getElementById(`thumbnail-${displaySetInstanceUID}`);
      if (element && typeof element.scrollIntoView === 'function') {
        // TODO: Any way to support IE here?
        element.scrollIntoView({
          behavior: 'smooth'
        });
        setJumpToDisplaySet(null);
      }
    }
  }, [jumpToDisplaySet, expandedStudyInstanceUIDs, activeTabName]);
  (0,react.useEffect)(() => {
    if (!jumpToDisplaySet) {
      return;
    }
    const displaySetInstanceUID = jumpToDisplaySet;
    // Set the activeTabName and expand the study
    const thumbnailLocation = _findTabAndStudyOfDisplaySet(displaySetInstanceUID, tabs);
    if (!thumbnailLocation) {
      console.warn('jumpToThumbnail: displaySet thumbnail not found.');
      return;
    }
    const {
      tabName,
      StudyInstanceUID
    } = thumbnailLocation;
    setActiveTabName(tabName);
    const studyExpanded = expandedStudyInstanceUIDs.includes(StudyInstanceUID);
    if (!studyExpanded) {
      const updatedExpandedStudyInstanceUIDs = [...expandedStudyInstanceUIDs, StudyInstanceUID];
      setExpandedStudyInstanceUIDs(updatedExpandedStudyInstanceUIDs);
    }
  }, [expandedStudyInstanceUIDs, jumpToDisplaySet, tabs]);
  return /*#__PURE__*/react.createElement(ui_src/* StudyBrowser */.eX, {
    tabs: tabs,
    servicesManager: servicesManager,
    activeTabName: activeTabName,
    expandedStudyInstanceUIDs: expandedStudyInstanceUIDs,
    onClickStudy: _handleStudyClick,
    onClickTab: clickedTabName => {
      setActiveTabName(clickedTabName);
    },
    onClickUntrack: displaySetInstanceUID => {
      const displaySet = displaySetService.getDisplaySetByUID(displaySetInstanceUID);
      // TODO: shift this somewhere else where we're centralizing this logic?
      // Potentially a helper from displaySetInstanceUID to this
      sendTrackedMeasurementsEvent('UNTRACK_SERIES', {
        SeriesInstanceUID: displaySet.SeriesInstanceUID
      });
    },
    onClickThumbnail: () => {},
    onDoubleClickThumbnail: onDoubleClickThumbnailHandler,
    activeDisplaySetInstanceUIDs: activeViewportDisplaySetInstanceUIDs
  });
}
PanelStudyBrowserTracking.propTypes = {
  servicesManager: (prop_types_default()).object.isRequired,
  dataSource: prop_types_default().shape({
    getImageIdsForDisplaySet: (prop_types_default()).func.isRequired
  }).isRequired,
  getImageSrc: (prop_types_default()).func.isRequired,
  getStudiesForPatientByMRN: (prop_types_default()).func.isRequired,
  requestDisplaySetCreationForStudy: (prop_types_default()).func.isRequired
};
/* harmony default export */ const PanelStudyBrowserTracking_PanelStudyBrowserTracking = (PanelStudyBrowserTracking);

/**
 * Maps from the DataSource's format to a naturalized object
 *
 * @param {*} studies
 */
function _mapDataSourceStudies(studies) {
  return studies.map(study => {
    // TODO: Why does the data source return in this format?
    return {
      AccessionNumber: study.accession,
      StudyDate: study.date,
      StudyDescription: study.description,
      NumInstances: study.instances,
      ModalitiesInStudy: study.modalities,
      PatientID: study.mrn,
      PatientName: study.patientName,
      StudyInstanceUID: study.studyInstanceUid,
      StudyTime: study.time
    };
  });
}
function _mapDisplaySets(displaySets, thumbnailImageSrcMap, trackedSeriesInstanceUIDs, viewports,
// TODO: make array of `displaySetInstanceUIDs`?
viewportGridService, dataSource, displaySetService, uiDialogService, uiNotificationService) {
  const thumbnailDisplaySets = [];
  const thumbnailNoImageDisplaySets = [];
  displaySets.filter(ds => !ds.excludeFromThumbnailBrowser).forEach(ds => {
    const imageSrc = thumbnailImageSrcMap[ds.displaySetInstanceUID];
    const componentType = _getComponentType(ds);
    const numPanes = viewportGridService.getNumViewportPanes();
    const viewportIdentificator = [];
    if (numPanes !== 1) {
      viewports.forEach(viewportData => {
        if (viewportData?.displaySetInstanceUIDs?.includes(ds.displaySetInstanceUID)) {
          viewportIdentificator.push(viewportData.viewportLabel);
        }
      });
    }
    const array = componentType === 'thumbnailTracked' ? thumbnailDisplaySets : thumbnailNoImageDisplaySets;
    const {
      displaySetInstanceUID
    } = ds;
    const thumbnailProps = {
      displaySetInstanceUID,
      description: ds.SeriesDescription,
      seriesNumber: ds.SeriesNumber,
      modality: ds.Modality,
      seriesDate: formatDate(ds.SeriesDate),
      numInstances: ds.numImageFrames,
      countIcon: ds.countIcon,
      messages: ds.messages,
      StudyInstanceUID: ds.StudyInstanceUID,
      componentType,
      imageSrc,
      dragData: {
        type: 'displayset',
        displaySetInstanceUID
        // .. Any other data to pass
      },

      isTracked: trackedSeriesInstanceUIDs.includes(ds.SeriesInstanceUID),
      isHydratedForDerivedDisplaySet: ds.isHydrated,
      viewportIdentificator
    };
    if (componentType === 'thumbnailNoImage') {
      if (dataSource.reject && dataSource.reject.series) {
        thumbnailProps.canReject = !ds?.unsupported;
        thumbnailProps.onReject = () => {
          uiDialogService.create({
            id: 'ds-reject-sr',
            centralize: true,
            isDraggable: false,
            showOverlay: true,
            content: ui_src/* Dialog */.Vq,
            contentProps: {
              title: 'Delete Report',
              body: () => /*#__PURE__*/react.createElement("div", {
                className: "bg-primary-dark p-4 text-white"
              }, /*#__PURE__*/react.createElement("p", null, "Are you sure you want to delete this report?"), /*#__PURE__*/react.createElement("p", {
                className: "mt-2"
              }, "This action cannot be undone.")),
              actions: [{
                id: 'cancel',
                text: 'Cancel',
                type: ui_src/* ButtonEnums.type */.LZ.dt.secondary
              }, {
                id: 'yes',
                text: 'Yes',
                type: ui_src/* ButtonEnums.type */.LZ.dt.primary,
                classes: ['reject-yes-button']
              }],
              onClose: () => uiDialogService.dismiss({
                id: 'ds-reject-sr'
              }),
              onShow: () => {
                const yesButton = document.querySelector('.reject-yes-button');
                yesButton.focus();
              },
              onSubmit: async _ref2 => {
                let {
                  action
                } = _ref2;
                switch (action.id) {
                  case 'yes':
                    try {
                      await dataSource.reject.series(ds.StudyInstanceUID, ds.SeriesInstanceUID);
                      displaySetService.deleteDisplaySet(displaySetInstanceUID);
                      uiDialogService.dismiss({
                        id: 'ds-reject-sr'
                      });
                      uiNotificationService.show({
                        title: 'Delete Report',
                        message: 'Report deleted successfully',
                        type: 'success'
                      });
                    } catch (error) {
                      uiDialogService.dismiss({
                        id: 'ds-reject-sr'
                      });
                      uiNotificationService.show({
                        title: 'Delete Report',
                        message: 'Failed to delete report',
                        type: 'error'
                      });
                    }
                    break;
                  case 'cancel':
                    uiDialogService.dismiss({
                      id: 'ds-reject-sr'
                    });
                    break;
                }
              }
            }
          });
        };
      } else {
        thumbnailProps.canReject = false;
      }
    }
    array.push(thumbnailProps);
  });
  return [...thumbnailDisplaySets, ...thumbnailNoImageDisplaySets];
}
const thumbnailNoImageModalities = ['SR', 'SEG', 'SM', 'RTSTRUCT', 'RTPLAN', 'RTDOSE', 'DOC', 'OT'];
function _getComponentType(ds) {
  if (thumbnailNoImageModalities.includes(ds.Modality) || ds?.unsupported) {
    return 'thumbnailNoImage';
  }
  return 'thumbnailTracked';
}

/**
 *
 * @param {string[]} primaryStudyInstanceUIDs
 * @param {object[]} studyDisplayList
 * @param {string} studyDisplayList.studyInstanceUid
 * @param {string} studyDisplayList.date
 * @param {string} studyDisplayList.description
 * @param {string} studyDisplayList.modalities
 * @param {number} studyDisplayList.numInstances
 * @param {object[]} displaySets
 * @returns tabs - The prop object expected by the StudyBrowser component
 */
function _createStudyBrowserTabs(primaryStudyInstanceUIDs, studyDisplayList, displaySets, hangingProtocolService) {
  const primaryStudies = [];
  const recentStudies = [];
  const allStudies = [];

  // Iterate over each study...
  studyDisplayList.forEach(study => {
    // Find it's display sets
    const displaySetsForStudy = displaySets.filter(ds => ds.StudyInstanceUID === study.studyInstanceUid);

    // Sort them
    const dsSortFn = hangingProtocolService.getDisplaySetSortFunction();
    displaySetsForStudy.sort(dsSortFn);

    /* Sort by series number, then by series date
      displaySetsForStudy.sort((a, b) => {
        if (a.seriesNumber !== b.seriesNumber) {
          return a.seriesNumber - b.seriesNumber;
        }
         const seriesDateA = Date.parse(a.seriesDate);
        const seriesDateB = Date.parse(b.seriesDate);
         return seriesDateA - seriesDateB;
      });
    */

    // Map the study to it's tab/view representation
    const tabStudy = Object.assign({}, study, {
      displaySets: displaySetsForStudy
    });

    // Add the "tab study" to the 'primary', 'recent', and/or 'all' tab group(s)
    if (primaryStudyInstanceUIDs.includes(study.studyInstanceUid)) {
      primaryStudies.push(tabStudy);
      allStudies.push(tabStudy);
    } else {
      // TODO: Filter allStudies to dates within one year of current date
      recentStudies.push(tabStudy);
      allStudies.push(tabStudy);
    }
  });

  // Newest first
  const _byDate = (a, b) => {
    const dateA = Date.parse(a);
    const dateB = Date.parse(b);
    return dateB - dateA;
  };
  const tabs = [{
    name: 'primary',
    label: 'Primary',
    studies: primaryStudies.sort((studyA, studyB) => _byDate(studyA.date, studyB.date))
  }, {
    name: 'recent',
    label: 'Recent',
    studies: recentStudies.sort((studyA, studyB) => _byDate(studyA.date, studyB.date))
  }, {
    name: 'all',
    label: 'All',
    studies: allStudies.sort((studyA, studyB) => _byDate(studyA.date, studyB.date))
  }];
  return tabs;
}
function _findTabAndStudyOfDisplaySet(displaySetInstanceUID, tabs) {
  for (let t = 0; t < tabs.length; t++) {
    const {
      studies
    } = tabs[t];
    for (let s = 0; s < studies.length; s++) {
      const {
        displaySets
      } = studies[s];
      for (let d = 0; d < displaySets.length; d++) {
        const displaySet = displaySets[d];
        if (displaySet.displaySetInstanceUID === displaySetInstanceUID) {
          return {
            tabName: tabs[t].name,
            StudyInstanceUID: studies[s].studyInstanceUid
          };
        }
      }
    }
  }
}
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/panels/PanelStudyBrowserTracking/getImageSrcFromImageId.js
/**
 * @param {*} cornerstone
 * @param {*} imageId
 */
function getImageSrcFromImageId(cornerstone, imageId) {
  return new Promise((resolve, reject) => {
    const canvas = document.createElement('canvas');
    cornerstone.utilities.loadImageToCanvas({
      canvas,
      imageId
    }).then(imageId => {
      resolve(canvas.toDataURL());
    }).catch(reject);
  });
}
/* harmony default export */ const PanelStudyBrowserTracking_getImageSrcFromImageId = (getImageSrcFromImageId);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/panels/PanelStudyBrowserTracking/requestDisplaySetCreationForStudy.js
function requestDisplaySetCreationForStudy(dataSource, displaySetService, StudyInstanceUID, madeInClient) {
  if (displaySetService.activeDisplaySets.some(displaySet => displaySet.StudyInstanceUID === StudyInstanceUID)) {
    return;
  }
  dataSource.retrieve.series.metadata({
    StudyInstanceUID,
    madeInClient
  });
}
/* harmony default export */ const PanelStudyBrowserTracking_requestDisplaySetCreationForStudy = (requestDisplaySetCreationForStudy);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/panels/PanelStudyBrowserTracking/index.tsx


//



function _getStudyForPatientUtility(extensionManager) {
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-default.utilityModule.common');
  const {
    getStudiesForPatientByMRN
  } = utilityModule.exports;
  return getStudiesForPatientByMRN;
}

/**
 * Wraps the PanelStudyBrowser and provides features afforded by managers/services
 *
 * @param {object} params
 * @param {object} commandsManager
 * @param {object} extensionManager
 */
function WrappedPanelStudyBrowserTracking(_ref) {
  let {
    commandsManager,
    extensionManager,
    servicesManager
  } = _ref;
  const dataSource = extensionManager.getActiveDataSource()[0];
  const getStudiesForPatientByMRN = _getStudyForPatientUtility(extensionManager);
  const _getStudiesForPatientByMRN = getStudiesForPatientByMRN.bind(null, dataSource);
  const _getImageSrcFromImageId = _createGetImageSrcFromImageIdFn(extensionManager);
  const _requestDisplaySetCreationForStudy = PanelStudyBrowserTracking_requestDisplaySetCreationForStudy.bind(null, dataSource);
  return /*#__PURE__*/react.createElement(PanelStudyBrowserTracking_PanelStudyBrowserTracking, {
    servicesManager: servicesManager,
    dataSource: dataSource,
    getImageSrc: _getImageSrcFromImageId,
    getStudiesForPatientByMRN: _getStudiesForPatientByMRN,
    requestDisplaySetCreationForStudy: _requestDisplaySetCreationForStudy
  });
}

/**
 * Grabs cornerstone library reference using a dependent command from
 * the @ohif/extension-cornerstone extension. Then creates a helper function
 * that can take an imageId and return an image src.
 *
 * @param {func} getCommand - CommandManager's getCommand method
 * @returns {func} getImageSrcFromImageId - A utility function powered by
 * cornerstone
 */
function _createGetImageSrcFromImageIdFn(extensionManager) {
  const utilities = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.common');
  try {
    const {
      cornerstone
    } = utilities.exports.getCornerstoneLibraries();
    return PanelStudyBrowserTracking_getImageSrcFromImageId.bind(null, cornerstone);
  } catch (ex) {
    throw new Error('Required command not found');
  }
}
WrappedPanelStudyBrowserTracking.propTypes = {
  commandsManager: (prop_types_default()).object.isRequired,
  extensionManager: (prop_types_default()).object.isRequired,
  servicesManager: (prop_types_default()).object.isRequired
};
/* harmony default export */ const panels_PanelStudyBrowserTracking = (WrappedPanelStudyBrowserTracking);
// EXTERNAL MODULE: ./hooks/index.js + 1 modules
var hooks = __webpack_require__(10800);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/panels/PanelMeasurementTableTracking/ActionButtons.tsx




function ActionButtons(_ref) {
  let {
    onExportClick,
    onCreateReportClick,
    disabled
  } = _ref;
  const {
    t
  } = (0,es/* useTranslation */.$G)('MeasurementTable');
  return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement(ui_src/* Button */.zx, {
    onClick: onExportClick,
    disabled: disabled,
    type: ui_src/* ButtonEnums.type */.LZ.dt.secondary,
    size: ui_src/* ButtonEnums.size */.LZ.dp.small
  }, t('Export')), /*#__PURE__*/react.createElement(ui_src/* Button */.zx, {
    className: "ml-2",
    onClick: onCreateReportClick,
    type: ui_src/* ButtonEnums.type */.LZ.dt.secondary,
    size: ui_src/* ButtonEnums.size */.LZ.dp.small,
    disabled: disabled
  }, t('Create Report')));
}
ActionButtons.propTypes = {
  onExportClick: (prop_types_default()).func,
  onCreateReportClick: (prop_types_default()).func,
  disabled: (prop_types_default()).bool
};
ActionButtons.defaultProps = {
  onExportClick: () => alert('Export'),
  onCreateReportClick: () => alert('Create Report'),
  disabled: false
};
/* harmony default export */ const PanelMeasurementTableTracking_ActionButtons = (ActionButtons);
// EXTERNAL MODULE: ../../../node_modules/lodash.debounce/index.js
var lodash_debounce = __webpack_require__(8324);
var lodash_debounce_default = /*#__PURE__*/__webpack_require__.n(lodash_debounce);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/panels/PanelMeasurementTableTracking/index.tsx








const {
  downloadCSVReport
} = src.utils;
const {
  formatDate: PanelMeasurementTableTracking_formatDate
} = src.utils;
const DISPLAY_STUDY_SUMMARY_INITIAL_VALUE = {
  key: undefined,
  //
  date: '',
  // '07-Sep-2010',
  modality: '',
  // 'CT',
  description: '' // 'CHEST/ABD/PELVIS W CONTRAST',
};

function PanelMeasurementTableTracking(_ref) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const [viewportGrid] = (0,ui_src/* useViewportGrid */.O_)();
  const [measurementChangeTimestamp, setMeasurementsUpdated] = (0,react.useState)(Date.now().toString());
  const debouncedMeasurementChangeTimestamp = (0,hooks/* useDebounce */.N)(measurementChangeTimestamp, 200);
  const {
    measurementService,
    uiDialogService,
    displaySetService
  } = servicesManager.services;
  const [trackedMeasurements, sendTrackedMeasurementsEvent] = (0,getContextModule/* useTrackedMeasurements */.I)();
  const {
    trackedStudy,
    trackedSeries
  } = trackedMeasurements.context;
  const [displayStudySummary, setDisplayStudySummary] = (0,react.useState)(DISPLAY_STUDY_SUMMARY_INITIAL_VALUE);
  const [displayMeasurements, setDisplayMeasurements] = (0,react.useState)([]);
  const measurementsPanelRef = (0,react.useRef)(null);
  (0,react.useEffect)(() => {
    const measurements = measurementService.getMeasurements();
    const filteredMeasurements = measurements.filter(m => trackedStudy === m.referenceStudyUID && trackedSeries.includes(m.referenceSeriesUID));
    const mappedMeasurements = filteredMeasurements.map(m => _mapMeasurementToDisplay(m, measurementService.VALUE_TYPES, displaySetService));
    setDisplayMeasurements(mappedMeasurements);
    // eslint-ignore-next-line
  }, [measurementService, trackedStudy, trackedSeries, debouncedMeasurementChangeTimestamp]);
  const updateDisplayStudySummary = async () => {
    if (trackedMeasurements.matches('tracking')) {
      const StudyInstanceUID = trackedStudy;
      const studyMeta = src.DicomMetadataStore.getStudy(StudyInstanceUID);
      const instanceMeta = studyMeta.series[0].instances[0];
      const {
        StudyDate,
        StudyDescription
      } = instanceMeta;
      const modalities = new Set();
      studyMeta.series.forEach(series => {
        if (trackedSeries.includes(series.SeriesInstanceUID)) {
          modalities.add(series.instances[0].Modality);
        }
      });
      const modality = Array.from(modalities).join('/');
      if (displayStudySummary.key !== StudyInstanceUID) {
        setDisplayStudySummary({
          key: StudyInstanceUID,
          date: StudyDate,
          // TODO: Format: '07-Sep-2010'
          modality,
          description: StudyDescription
        });
      }
    } else if (trackedStudy === '' || trackedStudy === undefined) {
      setDisplayStudySummary(DISPLAY_STUDY_SUMMARY_INITIAL_VALUE);
    }
  };

  // ~~ DisplayStudySummary
  (0,react.useEffect)(() => {
    updateDisplayStudySummary();
  }, [displayStudySummary.key, trackedMeasurements, trackedStudy, updateDisplayStudySummary]);

  // TODO: Better way to consolidated, debounce, check on change?
  // Are we exposing the right API for measurementService?
  // This watches for ALL measurementService changes. It updates a timestamp,
  // which is debounced. After a brief period of inactivity, this triggers
  // a re-render where we grab up-to-date measurements
  (0,react.useEffect)(() => {
    const added = measurementService.EVENTS.MEASUREMENT_ADDED;
    const addedRaw = measurementService.EVENTS.RAW_MEASUREMENT_ADDED;
    const updated = measurementService.EVENTS.MEASUREMENT_UPDATED;
    const removed = measurementService.EVENTS.MEASUREMENT_REMOVED;
    const cleared = measurementService.EVENTS.MEASUREMENTS_CLEARED;
    const subscriptions = [];
    [added, addedRaw, updated, removed, cleared].forEach(evt => {
      subscriptions.push(measurementService.subscribe(evt, () => {
        setMeasurementsUpdated(Date.now().toString());
        if (evt === added) {
          lodash_debounce_default()(() => {
            measurementsPanelRef.current.scrollTop = measurementsPanelRef.current.scrollHeight;
          }, 300)();
        }
      }).unsubscribe);
    });
    return () => {
      subscriptions.forEach(unsub => {
        unsub();
      });
    };
  }, [measurementService, sendTrackedMeasurementsEvent]);
  async function exportReport() {
    const measurements = measurementService.getMeasurements();
    const trackedMeasurements = measurements.filter(m => trackedStudy === m.referenceStudyUID && trackedSeries.includes(m.referenceSeriesUID));
    downloadCSVReport(trackedMeasurements, measurementService);
  }
  const jumpToImage = _ref2 => {
    let {
      uid,
      isActive
    } = _ref2;
    measurementService.jumpToMeasurement(viewportGrid.activeViewportId, uid);
    onMeasurementItemClickHandler({
      uid,
      isActive
    });
  };
  const onMeasurementItemEditHandler = _ref3 => {
    let {
      uid,
      isActive
    } = _ref3;
    const measurement = measurementService.getMeasurement(uid);
    jumpToImage({
      uid,
      isActive
    });
    const onSubmitHandler = _ref4 => {
      let {
        action,
        value
      } = _ref4;
      switch (action.id) {
        case 'save':
          {
            measurementService.update(uid, {
              ...measurement,
              ...value
            }, true);
          }
      }
      uiDialogService.dismiss({
        id: 'enter-annotation'
      });
    };
    uiDialogService.create({
      id: 'enter-annotation',
      centralize: true,
      isDraggable: false,
      showOverlay: true,
      content: ui_src/* Dialog */.Vq,
      contentProps: {
        title: 'Annotation',
        noCloseButton: true,
        value: {
          label: measurement.label || ''
        },
        body: _ref5 => {
          let {
            value,
            setValue
          } = _ref5;
          const onChangeHandler = event => {
            event.persist();
            setValue(value => ({
              ...value,
              label: event.target.value
            }));
          };
          const onKeyPressHandler = event => {
            if (event.key === 'Enter') {
              onSubmitHandler({
                value,
                action: {
                  id: 'save'
                }
              });
            }
          };
          return /*#__PURE__*/react.createElement(ui_src/* Input */.II, {
            label: "Enter your annotation",
            labelClassName: "text-white grow text-[14px] leading-[1.2]",
            autoFocus: true,
            id: "annotation",
            className: "border-primary-main bg-black",
            type: "text",
            value: value.label,
            onChange: onChangeHandler,
            onKeyPress: onKeyPressHandler
          });
        },
        actions: [{
          id: 'cancel',
          text: 'Cancel',
          type: ui_src/* ButtonEnums.type */.LZ.dt.secondary
        }, {
          id: 'save',
          text: 'Save',
          type: ui_src/* ButtonEnums.type */.LZ.dt.primary
        }],
        onSubmit: onSubmitHandler
      }
    });
  };
  const onMeasurementItemClickHandler = _ref6 => {
    let {
      uid,
      isActive
    } = _ref6;
    if (!isActive) {
      const measurements = [...displayMeasurements];
      const measurement = measurements.find(m => m.uid === uid);
      measurements.forEach(m => m.isActive = m.uid !== uid ? false : true);
      measurement.isActive = true;
      setDisplayMeasurements(measurements);
    }
  };
  const displayMeasurementsWithoutFindings = displayMeasurements.filter(dm => dm.measurementType !== measurementService.VALUE_TYPES.POINT);
  const additionalFindings = displayMeasurements.filter(dm => dm.measurementType === measurementService.VALUE_TYPES.POINT);
  return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("div", {
    className: "invisible-scrollbar overflow-y-auto overflow-x-hidden",
    ref: measurementsPanelRef,
    "data-cy": 'trackedMeasurements-panel'
  }, displayStudySummary.key && /*#__PURE__*/react.createElement(ui_src/* StudySummary */.YL, {
    date: PanelMeasurementTableTracking_formatDate(displayStudySummary.date),
    modality: displayStudySummary.modality,
    description: displayStudySummary.description
  }), /*#__PURE__*/react.createElement(ui_src/* MeasurementTable */.wt, {
    title: "Measurements",
    data: displayMeasurementsWithoutFindings,
    servicesManager: servicesManager,
    onClick: jumpToImage,
    onEdit: onMeasurementItemEditHandler
  }), additionalFindings.length !== 0 && /*#__PURE__*/react.createElement(ui_src/* MeasurementTable */.wt, {
    title: "Additional Findings",
    data: additionalFindings,
    servicesManager: servicesManager,
    onClick: jumpToImage,
    onEdit: onMeasurementItemEditHandler
  })), /*#__PURE__*/react.createElement("div", {
    className: "flex justify-center p-4"
  }, /*#__PURE__*/react.createElement(PanelMeasurementTableTracking_ActionButtons, {
    onExportClick: exportReport,
    onCreateReportClick: () => {
      sendTrackedMeasurementsEvent('SAVE_REPORT', {
        viewportId: viewportGrid.activeViewportId,
        isBackupSave: true
      });
    },
    disabled: additionalFindings.length === 0 && displayMeasurementsWithoutFindings.length === 0
  })));
}
PanelMeasurementTableTracking.propTypes = {
  servicesManager: prop_types_default().shape({
    services: prop_types_default().shape({
      measurementService: prop_types_default().shape({
        getMeasurements: (prop_types_default()).func.isRequired,
        VALUE_TYPES: (prop_types_default()).object.isRequired
      }).isRequired
    }).isRequired
  }).isRequired
};

// TODO: This could be a measurementService mapper
function _mapMeasurementToDisplay(measurement, types, displaySetService) {
  const {
    referenceStudyUID,
    referenceSeriesUID,
    SOPInstanceUID
  } = measurement;

  // TODO: We don't deal with multiframe well yet, would need to update
  // This in OHIF-312 when we add FrameIndex to measurements.

  const instance = src.DicomMetadataStore.getInstance(referenceStudyUID, referenceSeriesUID, SOPInstanceUID);
  const displaySets = displaySetService.getDisplaySetsForSeries(referenceSeriesUID);
  if (!displaySets[0] || !displaySets[0].images) {
    throw new Error('The tracked measurements panel should only be tracking "stack" displaySets.');
  }
  const {
    displayText: baseDisplayText,
    uid,
    label: baseLabel,
    type,
    selected,
    findingSites,
    finding
  } = measurement;
  const firstSite = findingSites?.[0];
  const label = baseLabel || finding?.text || firstSite?.text || '(empty)';
  let displayText = baseDisplayText || [];
  if (findingSites) {
    const siteText = [];
    findingSites.forEach(site => {
      if (site?.text !== label) {
        siteText.push(site.text);
      }
    });
    displayText = [...siteText, ...displayText];
  }
  if (finding && finding?.text !== label) {
    displayText = [finding.text, ...displayText];
  }
  return {
    uid,
    label,
    baseLabel,
    measurementType: type,
    displayText,
    baseDisplayText,
    isActive: selected,
    finding,
    findingSites
  };
}
/* harmony default export */ const panels_PanelMeasurementTableTracking = (PanelMeasurementTableTracking);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/panels/index.js



;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/getPanelModule.tsx


// TODO:
// - No loading UI exists yet
// - cancel promises when component is destroyed
// - show errors in UI for thumbnails if promise fails
function getPanelModule(_ref) {
  let {
    commandsManager,
    extensionManager,
    servicesManager
  } = _ref;
  return [{
    name: 'seriesList',
    iconName: 'tab-studies',
    iconLabel: 'Studies',
    label: 'Studies',
    component: panels_PanelStudyBrowserTracking.bind(null, {
      commandsManager,
      extensionManager,
      servicesManager
    })
  }, {
    name: 'trackedMeasurements',
    iconName: 'tab-linear',
    iconLabel: 'Measure',
    label: 'Measurements',
    component: panels_PanelMeasurementTableTracking.bind(null, {
      commandsManager,
      extensionManager,
      servicesManager
    })
  }];
}
/* harmony default export */ const src_getPanelModule = (getPanelModule);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/getViewportModule.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }

const Component = /*#__PURE__*/react.lazy(() => {
  return __webpack_require__.e(/* import() */ 822).then(__webpack_require__.bind(__webpack_require__, 86822));
});
const OHIFCornerstoneViewport = props => {
  return /*#__PURE__*/react.createElement(react.Suspense, {
    fallback: /*#__PURE__*/react.createElement("div", null, "Loading...")
  }, /*#__PURE__*/react.createElement(Component, props));
};
function getViewportModule(_ref) {
  let {
    servicesManager,
    commandsManager,
    extensionManager
  } = _ref;
  const ExtendedOHIFCornerstoneTrackingViewport = props => {
    return /*#__PURE__*/react.createElement(OHIFCornerstoneViewport, _extends({
      servicesManager: servicesManager,
      commandsManager: commandsManager,
      extensionManager: extensionManager
    }, props));
  };
  return [{
    name: 'cornerstone-tracked',
    component: ExtendedOHIFCornerstoneTrackingViewport
  }];
}
/* harmony default export */ const src_getViewportModule = (getViewportModule);
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-measurement-tracking"}');
;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../extensions/measurement-tracking/src/index.tsx




const measurementTrackingExtension = {
  /**
   * Only required property. Should be a unique value across all extensions.
   */
  id: id,
  getContextModule: getContextModule/* default */.Z,
  getPanelModule: src_getPanelModule,
  getViewportModule: src_getViewportModule
};
/* harmony default export */ const measurement_tracking_src = (measurementTrackingExtension);

/***/ })

}]);