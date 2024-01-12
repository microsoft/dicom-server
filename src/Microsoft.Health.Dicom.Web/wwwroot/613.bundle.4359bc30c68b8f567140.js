"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[613],{

/***/ 32613:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ segmentation_src)
});

// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
;// CONCATENATED MODULE: ../../../modes/segmentation/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/mode-segmentation"}');
;// CONCATENATED MODULE: ../../../modes/segmentation/src/id.js

const id = package_namespaceObject.u2;

// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../modes/segmentation/src/toolbarButtons.ts


const {
  windowLevelPresets
} = src.defaults;
/**
 *
 * @param {*} type - 'tool' | 'action' | 'toggle'
 * @param {*} id
 * @param {*} icon
 * @param {*} label
 */
function _createButton(type, id, icon, label, commands, tooltip, uiType) {
  return {
    id,
    icon,
    label,
    type,
    commands,
    tooltip,
    uiType
  };
}
const _createActionButton = _createButton.bind(null, 'action');
const _createToggleButton = _createButton.bind(null, 'toggle');
const _createToolButton = _createButton.bind(null, 'tool');

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
const toolbarButtons = [
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
      commandName: 'toggleStackImageSync',
      commandOptions: {},
      context: 'CORNERSTONE'
    }]), _createToggleButton('ReferenceLines', 'tool-referenceLines',
    // change this with the new icon
    'Reference Lines', [{
      commandName: 'toggleReferenceLines',
      commandOptions: {},
      context: 'CORNERSTONE'
    }]), _createToolButton('StackScroll', 'tool-stack-scroll', 'Stack Scroll', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'StackScroll'
      },
      context: 'CORNERSTONE'
    }], 'Stack Scroll'), _createActionButton('invert', 'tool-invert', 'Invert', [{
      commandName: 'invertViewport',
      commandOptions: {},
      context: 'CORNERSTONE'
    }], 'Invert Colors'), _createToggleButton('cine', 'tool-cine', 'Cine', [{
      commandName: 'toggleCine',
      context: 'CORNERSTONE'
    }], 'Cine'), _createToolButton('Magnify', 'tool-magnify', 'Magnify', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Magnify'
      },
      context: 'CORNERSTONE'
    }], 'Magnify'), _createActionButton('TagBrowser', 'list-bullets', 'Dicom Tag Browser', [{
      commandName: 'openDICOMTagViewer',
      commandOptions: {},
      context: 'DEFAULT'
    }], 'Dicom Tag Browser')]
  }
}];
/* harmony default export */ const src_toolbarButtons = (toolbarButtons);
;// CONCATENATED MODULE: ../../../modes/segmentation/src/initToolGroups.ts
const brushInstanceNames = {
  CircularBrush: 'CircularBrush',
  CircularEraser: 'CircularEraser',
  SphereBrush: 'SphereBrush',
  SphereEraser: 'SphereEraser',
  ThresholdCircularBrush: 'ThresholdCircularBrush',
  ThresholdSphereBrush: 'ThresholdSphereBrush'
};
const brushStrategies = {
  CircularBrush: 'FILL_INSIDE_CIRCLE',
  CircularEraser: 'ERASE_INSIDE_CIRCLE',
  SphereBrush: 'FILL_INSIDE_SPHERE',
  SphereEraser: 'ERASE_INSIDE_SPHERE',
  ThresholdCircularBrush: 'THRESHOLD_INSIDE_CIRCLE',
  ThresholdSphereBrush: 'THRESHOLD_INSIDE_SPHERE'
};
function createTools(utilityModule) {
  const {
    toolNames,
    Enums
  } = utilityModule.exports;
  return {
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
    passive: Object.keys(brushInstanceNames).map(brushName => ({
      toolName: brushName,
      parentTool: 'Brush',
      configuration: {
        activeStrategy: brushStrategies[brushName]
      }
    })).concat([{
      toolName: toolNames.CircleScissors
    }, {
      toolName: toolNames.RectangleScissors
    }, {
      toolName: toolNames.SphereScissors
    }, {
      toolName: toolNames.StackScroll
    }, {
      toolName: toolNames.Magnify
    }, {
      toolName: toolNames.SegmentationDisplay
    }]),
    disabled: [{
      toolName: toolNames.ReferenceLines
    }]
  };
}
function initDefaultToolGroup(extensionManager, toolGroupService, commandsManager, toolGroupId) {
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.tools');
  const tools = createTools(utilityModule);
  toolGroupService.createToolGroupAndAddTools(toolGroupId, tools);
}
function initMPRToolGroup(extensionManager, toolGroupService, commandsManager) {
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.tools');
  const tools = createTools(utilityModule);
  tools.disabled.push({
    toolName: utilityModule.exports.toolNames.Crosshairs,
    configuration: {
      viewportIndicators: false,
      autoPan: {
        enabled: false,
        panSize: 10
      }
    }
  }, {
    toolName: utilityModule.exports.toolNames.ReferenceLines
  });
  toolGroupService.createToolGroupAndAddTools('mpr', tools);
}
function initToolGroups(extensionManager, toolGroupService, commandsManager) {
  initDefaultToolGroup(extensionManager, toolGroupService, commandsManager, 'default');
  initMPRToolGroup(extensionManager, toolGroupService, commandsManager);
}
/* harmony default export */ const src_initToolGroups = (initToolGroups);
;// CONCATENATED MODULE: ../../../modes/segmentation/src/index.tsx




const ohif = {
  layout: '@ohif/extension-default.layoutTemplateModule.viewerLayout',
  sopClassHandler: '@ohif/extension-default.sopClassHandlerModule.stack',
  hangingProtocol: '@ohif/extension-default.hangingProtocolModule.default',
  leftPanel: '@ohif/extension-default.panelModule.seriesList',
  rightPanel: '@ohif/extension-default.panelModule.measure'
};
const cornerstone = {
  viewport: '@ohif/extension-cornerstone.viewportModule.cornerstone'
};
const segmentation = {
  panel: '@ohif/extension-cornerstone-dicom-seg.panelModule.panelSegmentation',
  panelTool: '@ohif/extension-cornerstone-dicom-seg.panelModule.panelSegmentationWithTools',
  sopClassHandler: '@ohif/extension-cornerstone-dicom-seg.sopClassHandlerModule.dicom-seg',
  viewport: '@ohif/extension-cornerstone-dicom-seg.viewportModule.dicom-seg'
};

/**
 * Just two dependencies to be able to render a viewport with panels in order
 * to make sure that the mode is working.
 */
const extensionDependencies = {
  '@ohif/extension-default': '^3.0.0',
  '@ohif/extension-cornerstone': '^3.0.0',
  '@ohif/extension-cornerstone-dicom-seg': '^3.0.0'
};
function modeFactory(_ref) {
  let {
    modeConfiguration
  } = _ref;
  return {
    /**
     * Mode ID, which should be unique among modes used by the viewer. This ID
     * is used to identify the mode in the viewer's state.
     */
    id: id,
    routeName: 'segmentation',
    /**
     * Mode name, which is displayed in the viewer's UI in the workList, for the
     * user to select the mode.
     */
    displayName: 'Segmentation',
    /**
     * Runs when the Mode Route is mounted to the DOM. Usually used to initialize
     * Services and other resources.
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
        toolGroupService
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
      toolbarService.createButtonSection('primary', ['Zoom', 'WindowLevel', 'Pan', 'Capture', 'Layout', 'MPR', 'Crosshairs', 'MoreTools']);
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
      toolGroupService.destroy();
      syncGroupService.destroy();
      segmentationService.destroy();
      cornerstoneViewportService.destroy();
    },
    /** */
    validationTags: {
      study: [],
      series: []
    },
    /**
     * A boolean return value that indicates whether the mode is valid for the
     * modalities of the selected studies. For instance a PET/CT mode should be
     */
    isValidMode: _ref4 => {
      let {
        modalities
      } = _ref4;
      return true;
    },
    /**
     * Mode Routes are used to define the mode's behavior. A list of Mode Route
     * that includes the mode's path and the layout to be used. The layout will
     * include the components that are used in the layout. For instance, if the
     * default layoutTemplate is used (id: '@ohif/extension-default.layoutTemplateModule.viewerLayout')
     * it will include the leftPanels, rightPanels, and viewports. However, if
     * you define another layoutTemplate that includes a Footer for instance,
     * you should provide the Footer component here too. Note: We use Strings
     * to reference the component's ID as they are registered in the internal
     * ExtensionManager. The template for the string is:
     * `${extensionId}.{moduleType}.${componentId}`.
     */
    routes: [{
      path: 'template',
      layoutTemplate: _ref5 => {
        let {
          location,
          servicesManager
        } = _ref5;
        return {
          id: ohif.layout,
          props: {
            leftPanels: [ohif.leftPanel],
            rightPanels: [segmentation.panelTool],
            viewports: [{
              namespace: cornerstone.viewport,
              displaySetsToDisplay: [ohif.sopClassHandler]
            }, {
              namespace: segmentation.viewport,
              displaySetsToDisplay: [segmentation.sopClassHandler]
            }]
          }
        };
      }
    }],
    /** List of extensions that are used by the mode */
    extensions: extensionDependencies,
    /** HangingProtocol used by the mode */
    // hangingProtocol: [''],
    /** SopClassHandlers used by the mode */
    sopClassHandlers: [ohif.sopClassHandler, segmentation.sopClassHandler],
    /** hotkeys for mode */
    hotkeys: [...src/* hotkeys */.dD.defaults.hotkeyBindings]
  };
}
const mode = {
  id: id,
  modeFactory,
  extensionDependencies
};
/* harmony default export */ const segmentation_src = (mode);

/***/ })

}]);