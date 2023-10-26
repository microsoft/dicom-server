(self["webpackChunk"] = self["webpackChunk"] || []).push([[370],{

/***/ 71522:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ longitudinal_src),
  initToolGroups: () => (/* reexport */ src_initToolGroups),
  toolbarButtons: () => (/* reexport */ src_toolbarButtons)
});

// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var esm = __webpack_require__(3743);
;// CONCATENATED MODULE: ../../../modes/longitudinal/src/toolbarButtons.ts
// TODO: torn, can either bake this here; or have to create a whole new button type
// Only ways that you can pass in a custom React component for render :l



const {
  windowLevelPresets
} = src.defaults;
const _createActionButton = src/* ToolbarService */.Ok._createButton.bind(null, 'action');
const _createToggleButton = src/* ToolbarService */.Ok._createButton.bind(null, 'toggle');
const _createToolButton = src/* ToolbarService */.Ok._createButton.bind(null, 'tool');

/**
 *
 * @param {*} preset - preset number (from above import)
 * @param {*} title
 * @param {*} subtitle
 */
function _createWwwcPreset(preset, title, subtitle) {
  return {
    id: preset.toString(),
    title,
    subtitle,
    type: 'action',
    commands: [{
      commandName: 'setWindowLevel',
      commandOptions: {
        ...windowLevelPresets[preset]
      },
      context: 'CORNERSTONE'
    }]
  };
}
const toolGroupIds = ['default', 'mpr', 'SRToolGroup'];

/**
 * Creates an array of 'setToolActive' commands for the given toolName - one for
 * each toolGroupId specified in toolGroupIds.
 * @param {string} toolName
 * @returns {Array} an array of 'setToolActive' commands
 */
function _createSetToolActiveCommands(toolName) {
  const temp = toolGroupIds.map(toolGroupId => ({
    commandName: 'setToolActive',
    commandOptions: {
      toolGroupId,
      toolName
    },
    context: 'CORNERSTONE'
  }));
  return temp;
}
const ReferenceLinesCommands = [{
  commandName: 'setSourceViewportForReferenceLinesTool',
  context: 'CORNERSTONE'
}, {
  commandName: 'setToolActive',
  commandOptions: {
    toolName: 'ReferenceLines'
  },
  context: 'CORNERSTONE'
}];
const toolbarButtons = [
// Measurement
{
  id: 'MeasurementTools',
  type: 'ohif.splitButton',
  props: {
    groupId: 'MeasurementTools',
    isRadio: true,
    // ?
    // Switch?
    primary: _createToolButton('Length', 'tool-length', 'Length', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Length'
      },
      context: 'CORNERSTONE'
    }, {
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'SRLength',
        toolGroupId: 'SRToolGroup'
      },
      // we can use the setToolActive command for this from Cornerstone commandsModule
      context: 'CORNERSTONE'
    }], 'Length'),
    secondary: {
      icon: 'chevron-down',
      label: '',
      isActive: true,
      tooltip: 'More Measure Tools'
    },
    items: [_createToolButton('Length', 'tool-length', 'Length', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Length'
      },
      context: 'CORNERSTONE'
    }, {
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'SRLength',
        toolGroupId: 'SRToolGroup'
      },
      // we can use the setToolActive command for this from Cornerstone commandsModule
      context: 'CORNERSTONE'
    }], 'Length Tool'), _createToolButton('Bidirectional', 'tool-bidirectional', 'Bidirectional', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Bidirectional'
      },
      context: 'CORNERSTONE'
    }, {
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'SRBidirectional',
        toolGroupId: 'SRToolGroup'
      },
      context: 'CORNERSTONE'
    }], 'Bidirectional Tool'), _createToolButton('ArrowAnnotate', 'tool-annotate', 'Annotation', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'ArrowAnnotate'
      },
      context: 'CORNERSTONE'
    }, {
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'SRArrowAnnotate',
        toolGroupId: 'SRToolGroup'
      },
      context: 'CORNERSTONE'
    }], 'Arrow Annotate'), _createToolButton('EllipticalROI', 'tool-elipse', 'Ellipse', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'EllipticalROI'
      },
      context: 'CORNERSTONE'
    }, {
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'SREllipticalROI',
        toolGroupId: 'SRToolGroup'
      },
      context: 'CORNERSTONE'
    }], 'Ellipse Tool'), _createToolButton('CircleROI', 'tool-circle', 'Circle', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'CircleROI'
      },
      context: 'CORNERSTONE'
    }, {
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'SRCircleROI',
        toolGroupId: 'SRToolGroup'
      },
      context: 'CORNERSTONE'
    }], 'Circle Tool')]
  }
},
// Zoom..
{
  id: 'Zoom',
  type: 'ohif.radioGroup',
  props: {
    type: 'tool',
    icon: 'tool-zoom',
    label: 'Zoom',
    commands: _createSetToolActiveCommands('Zoom')
  }
},
// Window Level + Presets...
{
  id: 'WindowLevel',
  type: 'ohif.splitButton',
  props: {
    groupId: 'WindowLevel',
    primary: _createToolButton('WindowLevel', 'tool-window-level', 'Window Level', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'WindowLevel'
      },
      context: 'CORNERSTONE'
    }], 'Window Level'),
    secondary: {
      icon: 'chevron-down',
      label: 'W/L Manual',
      isActive: true,
      tooltip: 'W/L Presets'
    },
    isAction: true,
    // ?
    renderer: ui_src/* WindowLevelMenuItem */.eJ,
    items: [_createWwwcPreset(1, 'Soft tissue', '400 / 40'), _createWwwcPreset(2, 'Lung', '1500 / -600'), _createWwwcPreset(3, 'Liver', '150 / 90'), _createWwwcPreset(4, 'Bone', '2500 / 480'), _createWwwcPreset(5, 'Brain', '80 / 40')]
  }
},
// Pan...
{
  id: 'Pan',
  type: 'ohif.radioGroup',
  props: {
    type: 'tool',
    icon: 'tool-move',
    label: 'Pan',
    commands: _createSetToolActiveCommands('Pan')
  }
}, {
  id: 'Capture',
  type: 'ohif.action',
  props: {
    icon: 'tool-capture',
    label: 'Capture',
    type: 'action',
    commands: [{
      commandName: 'showDownloadViewportModal',
      commandOptions: {},
      context: 'CORNERSTONE'
    }]
  }
}, {
  id: 'Layout',
  type: 'ohif.layoutSelector',
  props: {
    rows: 3,
    columns: 3
  }
}, {
  id: 'MPR',
  type: 'ohif.action',
  props: {
    type: 'toggle',
    icon: 'icon-mpr',
    label: 'MPR',
    commands: [{
      commandName: 'toggleHangingProtocol',
      commandOptions: {
        protocolId: 'mpr'
      },
      context: 'DEFAULT'
    }]
  }
}, {
  id: 'Crosshairs',
  type: 'ohif.radioGroup',
  props: {
    type: 'tool',
    icon: 'tool-crosshair',
    label: 'Crosshairs',
    commands: [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Crosshairs',
        toolGroupId: 'mpr'
      },
      context: 'CORNERSTONE'
    }]
  }
},
// More...
{
  id: 'MoreTools',
  type: 'ohif.splitButton',
  props: {
    isRadio: true,
    // ?
    groupId: 'MoreTools',
    primary: _createActionButton('Reset', 'tool-reset', 'Reset View', [{
      commandName: 'resetViewport',
      commandOptions: {},
      context: 'CORNERSTONE'
    }], 'Reset'),
    secondary: {
      icon: 'chevron-down',
      label: '',
      isActive: true,
      tooltip: 'More Tools'
    },
    items: [_createActionButton('Reset', 'tool-reset', 'Reset View', [{
      commandName: 'resetViewport',
      commandOptions: {},
      context: 'CORNERSTONE'
    }], 'Reset'), _createActionButton('rotate-right', 'tool-rotate-right', 'Rotate Right', [{
      commandName: 'rotateViewportCW',
      commandOptions: {},
      context: 'CORNERSTONE'
    }], 'Rotate +90'), _createActionButton('flip-horizontal', 'tool-flip-horizontal', 'Flip Horizontally', [{
      commandName: 'flipViewportHorizontal',
      commandOptions: {},
      context: 'CORNERSTONE'
    }], 'Flip Horizontal'), _createToggleButton('StackImageSync', 'link', 'Stack Image Sync', [{
      commandName: 'toggleStackImageSync'
    }], 'Enable position synchronization on stack viewports', {
      listeners: {
        [esm.EVENTS.STACK_VIEWPORT_NEW_STACK]: {
          commandName: 'toggleStackImageSync',
          commandOptions: {
            toggledState: true
          }
        }
      }
    }), _createToggleButton('ReferenceLines', 'tool-referenceLines',
    // change this with the new icon
    'Reference Lines', ReferenceLinesCommands, 'Show Reference Lines', {
      listeners: {
        [esm.EVENTS.STACK_VIEWPORT_NEW_STACK]: ReferenceLinesCommands,
        [esm.EVENTS.ACTIVE_VIEWPORT_ID_CHANGED]: ReferenceLinesCommands
      }
    }), _createToggleButton('ImageOverlayViewer', 'toggle-dicom-overlay', 'Image Overlay', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'ImageOverlayViewer'
      },
      context: 'CORNERSTONE'
    }], 'Image Overlay', {
      isActive: true
    }), _createToolButton('StackScroll', 'tool-stack-scroll', 'Stack Scroll', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'StackScroll'
      },
      context: 'CORNERSTONE'
    }], 'Stack Scroll'), _createActionButton('invert', 'tool-invert', 'Invert', [{
      commandName: 'invertViewport',
      commandOptions: {},
      context: 'CORNERSTONE'
    }], 'Invert Colors'), _createToolButton('Probe', 'tool-probe', 'Probe', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'DragProbe'
      },
      context: 'CORNERSTONE'
    }], 'Probe'), _createToggleButton('cine', 'tool-cine', 'Cine', [{
      commandName: 'toggleCine',
      context: 'CORNERSTONE'
    }], 'Cine'), _createToolButton('Angle', 'tool-angle', 'Angle', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Angle'
      },
      context: 'CORNERSTONE'
    }], 'Angle'),
    // Next two tools can be added once icons are added
    // _createToolButton(
    //   'Cobb Angle',
    //   'tool-cobb-angle',
    //   'Cobb Angle',
    //   [
    //     {
    //       commandName: 'setToolActive',
    //       commandOptions: {
    //         toolName: 'CobbAngle',
    //       },
    //       context: 'CORNERSTONE',
    //     },
    //   ],
    //   'Cobb Angle'
    // ),
    // _createToolButton(
    //   'Planar Freehand ROI',
    //   'tool-freehand',
    //   'PlanarFreehandROI',
    //   [
    //     {
    //       commandName: 'setToolActive',
    //       commandOptions: {
    //         toolName: 'PlanarFreehandROI',
    //       },
    //       context: 'CORNERSTONE',
    //     },
    //   ],
    //   'Planar Freehand ROI'
    // ),
    _createToolButton('Magnify', 'tool-magnify', 'Magnify', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Magnify'
      },
      context: 'CORNERSTONE'
    }], 'Magnify'), _createToolButton('Rectangle', 'tool-rectangle', 'Rectangle', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'RectangleROI'
      },
      context: 'CORNERSTONE'
    }], 'Rectangle'), _createToolButton('CalibrationLine', 'tool-calibration', 'Calibration', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'CalibrationLine'
      },
      context: 'CORNERSTONE'
    }], 'Calibration Line'), _createActionButton('TagBrowser', 'list-bullets', 'Dicom Tag Browser', [{
      commandName: 'openDICOMTagViewer',
      commandOptions: {},
      context: 'DEFAULT'
    }], 'Dicom Tag Browser')]
  }
}];
/* harmony default export */ const src_toolbarButtons = (toolbarButtons);
;// CONCATENATED MODULE: ../../../modes/longitudinal/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/mode-longitudinal"}');
;// CONCATENATED MODULE: ../../../modes/longitudinal/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../modes/longitudinal/src/initToolGroups.js
function initDefaultToolGroup(extensionManager, toolGroupService, commandsManager, toolGroupId) {
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.tools');
  const {
    toolNames,
    Enums
  } = utilityModule.exports;
  const tools = {
    active: [{
      toolName: toolNames.WindowLevel,
      bindings: [{
        mouseButton: Enums.MouseBindings.Primary
      }]
    }, {
      toolName: toolNames.Pan,
      bindings: [{
        mouseButton: Enums.MouseBindings.Auxiliary
      }]
    }, {
      toolName: toolNames.Zoom,
      bindings: [{
        mouseButton: Enums.MouseBindings.Secondary
      }]
    }, {
      toolName: toolNames.StackScrollMouseWheel,
      bindings: []
    }],
    passive: [{
      toolName: toolNames.Length
    }, {
      toolName: toolNames.ArrowAnnotate,
      configuration: {
        getTextCallback: (callback, eventDetails) => commandsManager.runCommand('arrowTextCallback', {
          callback,
          eventDetails
        }),
        changeTextCallback: (data, eventDetails, callback) => commandsManager.runCommand('arrowTextCallback', {
          callback,
          data,
          eventDetails
        })
      }
    }, {
      toolName: toolNames.Bidirectional
    }, {
      toolName: toolNames.DragProbe
    }, {
      toolName: toolNames.EllipticalROI
    }, {
      toolName: toolNames.CircleROI
    }, {
      toolName: toolNames.RectangleROI
    }, {
      toolName: toolNames.StackScroll
    }, {
      toolName: toolNames.Angle
    }, {
      toolName: toolNames.CobbAngle
    }, {
      toolName: toolNames.PlanarFreehandROI
    }, {
      toolName: toolNames.Magnify
    }, {
      toolName: toolNames.SegmentationDisplay
    }, {
      toolName: toolNames.CalibrationLine
    }],
    // enabled
    enabled: [{
      toolName: toolNames.ImageOverlayViewer
    }],
    // disabled
    disabled: [{
      toolName: toolNames.ReferenceLines
    }]
  };
  toolGroupService.createToolGroupAndAddTools(toolGroupId, tools);
}
function initSRToolGroup(extensionManager, toolGroupService, commandsManager) {
  const SRUtilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone-dicom-sr.utilityModule.tools');
  if (!SRUtilityModule) {
    return;
  }
  const CS3DUtilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.tools');
  const {
    toolNames: SRToolNames
  } = SRUtilityModule.exports;
  const {
    toolNames,
    Enums
  } = CS3DUtilityModule.exports;
  const tools = {
    active: [{
      toolName: toolNames.WindowLevel,
      bindings: [{
        mouseButton: Enums.MouseBindings.Primary
      }]
    }, {
      toolName: toolNames.Pan,
      bindings: [{
        mouseButton: Enums.MouseBindings.Auxiliary
      }]
    }, {
      toolName: toolNames.Zoom,
      bindings: [{
        mouseButton: Enums.MouseBindings.Secondary
      }]
    }, {
      toolName: toolNames.StackScrollMouseWheel,
      bindings: []
    }],
    passive: [{
      toolName: SRToolNames.SRLength
    }, {
      toolName: SRToolNames.SRArrowAnnotate
    }, {
      toolName: SRToolNames.SRBidirectional
    }, {
      toolName: SRToolNames.SREllipticalROI
    }, {
      toolName: SRToolNames.SRCircleROI
    }],
    enabled: [{
      toolName: SRToolNames.DICOMSRDisplay,
      bindings: []
    }]
    // disabled
  };

  const toolGroupId = 'SRToolGroup';
  toolGroupService.createToolGroupAndAddTools(toolGroupId, tools);
}
function initMPRToolGroup(extensionManager, toolGroupService, commandsManager) {
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.tools');
  const {
    toolNames,
    Enums
  } = utilityModule.exports;
  const tools = {
    active: [{
      toolName: toolNames.WindowLevel,
      bindings: [{
        mouseButton: Enums.MouseBindings.Primary
      }]
    }, {
      toolName: toolNames.Pan,
      bindings: [{
        mouseButton: Enums.MouseBindings.Auxiliary
      }]
    }, {
      toolName: toolNames.Zoom,
      bindings: [{
        mouseButton: Enums.MouseBindings.Secondary
      }]
    }, {
      toolName: toolNames.StackScrollMouseWheel,
      bindings: []
    }],
    passive: [{
      toolName: toolNames.Length
    }, {
      toolName: toolNames.ArrowAnnotate,
      configuration: {
        getTextCallback: (callback, eventDetails) => commandsManager.runCommand('arrowTextCallback', {
          callback,
          eventDetails
        }),
        changeTextCallback: (data, eventDetails, callback) => commandsManager.runCommand('arrowTextCallback', {
          callback,
          data,
          eventDetails
        })
      }
    }, {
      toolName: toolNames.Bidirectional
    }, {
      toolName: toolNames.DragProbe
    }, {
      toolName: toolNames.EllipticalROI
    }, {
      toolName: toolNames.CircleROI
    }, {
      toolName: toolNames.RectangleROI
    }, {
      toolName: toolNames.StackScroll
    }, {
      toolName: toolNames.Angle
    }, {
      toolName: toolNames.CobbAngle
    }, {
      toolName: toolNames.PlanarFreehandROI
    }, {
      toolName: toolNames.SegmentationDisplay
    }],
    disabled: [{
      toolName: toolNames.Crosshairs,
      configuration: {
        viewportIndicators: false,
        autoPan: {
          enabled: false,
          panSize: 10
        }
      }
    }, {
      toolName: toolNames.ReferenceLines
    }]

    // enabled
    // disabled
  };

  toolGroupService.createToolGroupAndAddTools('mpr', tools);
}
function initVolume3DToolGroup(extensionManager, toolGroupService) {
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.tools');
  const {
    toolNames,
    Enums
  } = utilityModule.exports;
  const tools = {
    active: [{
      toolName: toolNames.TrackballRotateTool,
      bindings: [{
        mouseButton: Enums.MouseBindings.Primary
      }]
    }, {
      toolName: toolNames.Zoom,
      bindings: [{
        mouseButton: Enums.MouseBindings.Secondary
      }]
    }, {
      toolName: toolNames.Pan,
      bindings: [{
        mouseButton: Enums.MouseBindings.Auxiliary
      }]
    }]
  };
  toolGroupService.createToolGroupAndAddTools('volume3d', tools);
}
function initToolGroups(extensionManager, toolGroupService, commandsManager) {
  initDefaultToolGroup(extensionManager, toolGroupService, commandsManager, 'default');
  initSRToolGroup(extensionManager, toolGroupService, commandsManager);
  initMPRToolGroup(extensionManager, toolGroupService, commandsManager);
  initVolume3DToolGroup(extensionManager, toolGroupService);
}
/* harmony default export */ const src_initToolGroups = (initToolGroups);
;// CONCATENATED MODULE: ../../../modes/longitudinal/src/index.js





// Allow this mode by excluding non-imaging modalities such as SR, SEG
// Also, SM is not a simple imaging modalities, so exclude it.
const NON_IMAGE_MODALITIES = ['SM', 'ECG', 'SR', 'SEG', 'RTSTRUCT'];
const ohif = {
  layout: '@ohif/extension-default.layoutTemplateModule.viewerLayout',
  sopClassHandler: '@ohif/extension-default.sopClassHandlerModule.stack',
  thumbnailList: '@ohif/extension-default.panelModule.seriesList'
};
const tracked = {
  measurements: '@ohif/extension-measurement-tracking.panelModule.trackedMeasurements',
  thumbnailList: '@ohif/extension-measurement-tracking.panelModule.seriesList',
  viewport: '@ohif/extension-measurement-tracking.viewportModule.cornerstone-tracked'
};
const dicomsr = {
  sopClassHandler: '@ohif/extension-cornerstone-dicom-sr.sopClassHandlerModule.dicom-sr',
  viewport: '@ohif/extension-cornerstone-dicom-sr.viewportModule.dicom-sr'
};
const dicomvideo = {
  sopClassHandler: '@ohif/extension-dicom-video.sopClassHandlerModule.dicom-video',
  viewport: '@ohif/extension-dicom-video.viewportModule.dicom-video'
};
const dicompdf = {
  sopClassHandler: '@ohif/extension-dicom-pdf.sopClassHandlerModule.dicom-pdf',
  viewport: '@ohif/extension-dicom-pdf.viewportModule.dicom-pdf'
};
const dicomSeg = {
  sopClassHandler: '@ohif/extension-cornerstone-dicom-seg.sopClassHandlerModule.dicom-seg',
  viewport: '@ohif/extension-cornerstone-dicom-seg.viewportModule.dicom-seg',
  panel: '@ohif/extension-cornerstone-dicom-seg.panelModule.panelSegmentation'
};
const dicomRT = {
  viewport: '@ohif/extension-cornerstone-dicom-rt.viewportModule.dicom-rt',
  sopClassHandler: '@ohif/extension-cornerstone-dicom-rt.sopClassHandlerModule.dicom-rt'
};
const extensionDependencies = {
  // Can derive the versions at least process.env.from npm_package_version
  '@ohif/extension-default': '^3.0.0',
  '@ohif/extension-cornerstone': '^3.0.0',
  '@ohif/extension-measurement-tracking': '^3.0.0',
  '@ohif/extension-cornerstone-dicom-sr': '^3.0.0',
  '@ohif/extension-cornerstone-dicom-seg': '^3.0.0',
  '@ohif/extension-cornerstone-dicom-rt': '^3.0.0',
  '@ohif/extension-dicom-pdf': '^3.0.1',
  '@ohif/extension-dicom-video': '^3.0.1'
};
function modeFactory(_ref) {
  let {
    modeConfiguration
  } = _ref;
  let _activatePanelTriggersSubscriptions = [];
  return {
    // TODO: We're using this as a route segment
    // We should not be.
    id: id,
    routeName: 'viewer',
    displayName: 'Basic Viewer',
    /**
     * Lifecycle hooks
     */
    onModeEnter: _ref2 => {
      let {
        servicesManager,
        extensionManager,
        commandsManager
      } = _ref2;
      const {
        measurementService,
        toolbarService,
        toolGroupService,
        panelService,
        customizationService
      } = servicesManager.services;
      measurementService.clearMeasurements();

      // Init Default and SR ToolGroups
      src_initToolGroups(extensionManager, toolGroupService, commandsManager);
      let unsubscribe;
      const activateTool = () => {
        toolbarService.recordInteraction({
          groupId: 'WindowLevel',
          interactionType: 'tool',
          commands: [{
            commandName: 'setToolActive',
            commandOptions: {
              toolName: 'WindowLevel'
            },
            context: 'CORNERSTONE'
          }]
        });

        // We don't need to reset the active tool whenever a viewport is getting
        // added to the toolGroup.
        unsubscribe();
      };

      // Since we only have one viewport for the basic cs3d mode and it has
      // only one hanging protocol, we can just use the first viewport
      ({
        unsubscribe
      } = toolGroupService.subscribe(toolGroupService.EVENTS.VIEWPORT_ADDED, activateTool));
      toolbarService.init(extensionManager);
      toolbarService.addButtons(src_toolbarButtons);
      toolbarService.createButtonSection('primary', ['MeasurementTools', 'Zoom', 'WindowLevel', 'Pan', 'Capture', 'Layout', 'MPR', 'Crosshairs', 'MoreTools']);
      customizationService.addModeCustomizations([{
        id: 'segmentation.disableEditing',
        value: true
      }]);

      // // ActivatePanel event trigger for when a segmentation or measurement is added.
      // // Do not force activation so as to respect the state the user may have left the UI in.
      // _activatePanelTriggersSubscriptions = [
      //   ...panelService.addActivatePanelTriggers(dicomSeg.panel, [
      //     {
      //       sourcePubSubService: segmentationService,
      //       sourceEvents: [
      //         segmentationService.EVENTS.SEGMENTATION_PIXEL_DATA_CREATED,
      //       ],
      //     },
      //   ]),
      //   ...panelService.addActivatePanelTriggers(tracked.measurements, [
      //     {
      //       sourcePubSubService: measurementService,
      //       sourceEvents: [
      //         measurementService.EVENTS.MEASUREMENT_ADDED,
      //         measurementService.EVENTS.RAW_MEASUREMENT_ADDED,
      //       ],
      //     },
      //   ]),
      // ];
    },

    onModeExit: _ref3 => {
      let {
        servicesManager
      } = _ref3;
      const {
        toolGroupService,
        syncGroupService,
        toolbarService,
        segmentationService,
        cornerstoneViewportService
      } = servicesManager.services;
      _activatePanelTriggersSubscriptions.forEach(sub => sub.unsubscribe());
      _activatePanelTriggersSubscriptions = [];
      toolGroupService.destroy();
      syncGroupService.destroy();
      segmentationService.destroy();
      cornerstoneViewportService.destroy();
    },
    validationTags: {
      study: [],
      series: []
    },
    isValidMode: function (_ref4) {
      let {
        modalities
      } = _ref4;
      const modalities_list = modalities.split('\\');

      // Exclude non-image modalities
      return !!modalities_list.filter(modality => NON_IMAGE_MODALITIES.indexOf(modality) === -1).length;
    },
    routes: [{
      path: 'longitudinal',
      /*init: ({ servicesManager, extensionManager }) => {
        //defaultViewerRouteInit
      },*/
      layoutTemplate: () => {
        return {
          id: ohif.layout,
          props: {
            leftPanels: [tracked.thumbnailList],
            rightPanels: [dicomSeg.panel, tracked.measurements],
            rightPanelDefaultClosed: true,
            viewports: [{
              namespace: tracked.viewport,
              displaySetsToDisplay: [ohif.sopClassHandler]
            }, {
              namespace: dicomsr.viewport,
              displaySetsToDisplay: [dicomsr.sopClassHandler]
            }, {
              namespace: dicomvideo.viewport,
              displaySetsToDisplay: [dicomvideo.sopClassHandler]
            }, {
              namespace: dicompdf.viewport,
              displaySetsToDisplay: [dicompdf.sopClassHandler]
            }, {
              namespace: dicomSeg.viewport,
              displaySetsToDisplay: [dicomSeg.sopClassHandler]
            }, {
              namespace: dicomRT.viewport,
              displaySetsToDisplay: [dicomRT.sopClassHandler]
            }]
          }
        };
      }
    }],
    extensions: extensionDependencies,
    // Default protocol gets self-registered by default in the init
    hangingProtocol: 'default',
    // Order is important in sop class handlers when two handlers both use
    // the same sop class under different situations.  In that case, the more
    // general handler needs to come last.  For this case, the dicomvideo must
    // come first to remove video transfer syntax before ohif uses images
    sopClassHandlers: [dicomvideo.sopClassHandler, dicomSeg.sopClassHandler, ohif.sopClassHandler, dicompdf.sopClassHandler, dicomsr.sopClassHandler, dicomRT.sopClassHandler],
    hotkeys: [...src/* hotkeys */.dD.defaults.hotkeyBindings],
    ...modeConfiguration
  };
}
const mode = {
  id: id,
  modeFactory,
  extensionDependencies
};
/* harmony default export */ const longitudinal_src = (mode);


/***/ }),

/***/ 78753:
/***/ (() => {

/* (ignored) */

/***/ })

}]);