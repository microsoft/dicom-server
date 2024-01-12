(self["webpackChunk"] = self["webpackChunk"] || []).push([[99],{

/***/ 7395:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ basic_test_mode_src)
});

// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var esm = __webpack_require__(3743);
;// CONCATENATED MODULE: ../../../modes/basic-test-mode/src/toolbarButtons.ts
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
    commands: [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Zoom'
      },
      context: 'CORNERSTONE'
    }]
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
    commands: [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Pan'
      },
      context: 'CORNERSTONE'
    }]
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
  type: 'ohif.splitButton',
  props: {
    groupId: 'LayoutTools',
    isRadio: false,
    primary: {
      id: 'Layout',
      type: 'action',
      uiType: 'ohif.layoutSelector',
      icon: 'tool-layout',
      label: 'Grid Layout',
      props: {
        rows: 4,
        columns: 4,
        commands: [{
          commandName: 'setLayout',
          commandOptions: {},
          context: 'CORNERSTONE'
        }]
      }
    },
    secondary: {
      icon: 'chevron-down',
      label: '',
      isActive: true,
      tooltip: 'Hanging Protocols'
    },
    items: [{
      id: '2x2',
      type: 'action',
      label: '2x2',
      commands: [{
        commandName: 'setHangingProtocol',
        commandOptions: {
          protocolId: '@ohif/mnGrid',
          stageId: '2x2'
        },
        context: 'DEFAULT'
      }]
    }, {
      id: '3x1',
      type: 'action',
      label: '3x1',
      commands: [{
        commandName: 'setHangingProtocol',
        commandOptions: {
          protocolId: '@ohif/mnGrid',
          stageId: '3x1'
        },
        context: 'DEFAULT'
      }]
    }, {
      id: '2x1',
      type: 'action',
      label: '2x1',
      commands: [{
        commandName: 'setHangingProtocol',
        commandOptions: {
          protocolId: '@ohif/mnGrid',
          stageId: '2x1'
        },
        context: 'DEFAULT'
      }]
    }, {
      id: '1x1',
      type: 'action',
      label: '1x1',
      commands: [{
        commandName: 'setHangingProtocol',
        commandOptions: {
          protocolId: '@ohif/mnGrid',
          stageId: '1x1'
        },
        context: 'DEFAULT'
      }]
    }]
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
        toolGroupId: 'mpr',
        toolName: 'Crosshairs'
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
    }], 'Flip Horizontally'), _createToggleButton('StackImageSync', 'link', 'Stack Image Sync', [{
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
    }], 'Angle'), _createToolButton('Magnify', 'tool-magnify', 'Magnify', [{
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
    }], 'Rectangle'), _createActionButton('TagBrowser', 'list-bullets', 'Dicom Tag Browser', [{
      commandName: 'openDICOMTagViewer',
      commandOptions: {},
      context: 'DEFAULT'
    }], 'Dicom Tag Browser')]
  }
}];
/* harmony default export */ const src_toolbarButtons = (toolbarButtons);
;// CONCATENATED MODULE: ../../../modes/basic-test-mode/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/mode-test"}');
;// CONCATENATED MODULE: ../../../modes/basic-test-mode/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../modes/basic-test-mode/src/initToolGroups.ts
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
      toolName: toolNames.Magnify
    }, {
      toolName: toolNames.SegmentationDisplay
    }],
    // enabled
    // disabled
    disabled: [{
      toolName: toolNames.ReferenceLines
    }]
  };
  toolGroupService.createToolGroupAndAddTools(toolGroupId, tools);
}
function initSRToolGroup(extensionManager, toolGroupService, commandsManager) {
  const SRUtilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone-dicom-sr.utilityModule.tools');
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
function initToolGroups(extensionManager, toolGroupService, commandsManager) {
  initDefaultToolGroup(extensionManager, toolGroupService, commandsManager, 'default');
  initSRToolGroup(extensionManager, toolGroupService, commandsManager);
  initMPRToolGroup(extensionManager, toolGroupService, commandsManager);
}
/* harmony default export */ const src_initToolGroups = (initToolGroups);
;// CONCATENATED MODULE: ../../../modes/basic-test-mode/src/index.ts





// Allow this mode by excluding non-imaging modalities such as SR, SEG
// Also, SM is not a simple imaging modalities, so exclude it.
const NON_IMAGE_MODALITIES = ['SM', 'ECG', 'SR', 'SEG'];
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
const extensionDependencies = {
  // Can derive the versions at least process.env.from npm_package_version
  '@ohif/extension-default': '^3.0.0',
  '@ohif/extension-cornerstone': '^3.0.0',
  '@ohif/extension-measurement-tracking': '^3.0.0',
  '@ohif/extension-cornerstone-dicom-sr': '^3.0.0',
  '@ohif/extension-cornerstone-dicom-seg': '^3.0.0',
  '@ohif/extension-dicom-pdf': '^3.0.1',
  '@ohif/extension-dicom-video': '^3.0.1',
  '@ohif/extension-test': '^0.0.1'
};
function modeFactory() {
  return {
    // TODO: We're using this as a route segment
    // We should not be.
    id: id,
    routeName: 'basic-test',
    displayName: 'Basic Test Mode',
    /**
     * Lifecycle hooks
     */
    onModeEnter: _ref => {
      let {
        servicesManager,
        extensionManager,
        commandsManager
      } = _ref;
      const {
        measurementService,
        toolbarService,
        toolGroupService,
        customizationService
      } = servicesManager.services;
      measurementService.clearMeasurements();

      // Init Default and SR ToolGroups
      src_initToolGroups(extensionManager, toolGroupService, commandsManager);

      // init customizations
      customizationService.addModeCustomizations(['@ohif/extension-test.customizationModule.custom-context-menu']);
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
    },
    onModeExit: _ref2 => {
      let {
        servicesManager
      } = _ref2;
      const {
        toolGroupService,
        syncGroupService,
        segmentationService,
        cornerstoneViewportService
      } = servicesManager.services;
      toolGroupService.destroy();
      syncGroupService.destroy();
      segmentationService.destroy();
      cornerstoneViewportService.destroy();
    },
    validationTags: {
      study: [],
      series: []
    },
    isValidMode: function (_ref3) {
      let {
        modalities
      } = _ref3;
      const modalities_list = modalities.split('\\');

      // Exclude non-image modalities
      return !!modalities_list.filter(modality => NON_IMAGE_MODALITIES.indexOf(modality) === -1).length;
    },
    routes: [{
      path: 'basic-test',
      /*init: ({ servicesManager, extensionManager }) => {
        //defaultViewerRouteInit
      },*/
      layoutTemplate: () => {
        return {
          id: ohif.layout,
          props: {
            leftPanels: [tracked.thumbnailList],
            rightPanels: [dicomSeg.panel, tracked.measurements],
            // rightPanelDefaultClosed: true, // optional prop to start with collapse panels
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
    sopClassHandlers: [dicomvideo.sopClassHandler, dicomSeg.sopClassHandler, ohif.sopClassHandler, dicompdf.sopClassHandler, dicomsr.sopClassHandler],
    hotkeys: {
      // Don't store the hotkeys for basic-test-mode under the same key
      // because they get customized by tests
      name: 'basic-test-hotkeys',
      hotkeys: [...src/* hotkeys */.dD.defaults.hotkeyBindings]
    }
  };
}
const mode = {
  id: id,
  modeFactory,
  extensionDependencies
};
/* harmony default export */ const basic_test_mode_src = (mode);

/***/ }),

/***/ 78753:
/***/ (() => {

/* (ignored) */

/***/ })

}]);