"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[410],{

/***/ 15410:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ basic_dev_mode_src)
});

// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var src = __webpack_require__(71783);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var core_src = __webpack_require__(71771);
;// CONCATENATED MODULE: ../../../modes/basic-dev-mode/src/toolbarButtons.js
// TODO: torn, can either bake this here; or have to create a whole new button type
// Only ways that you can pass in a custom React component for render :l


const {
  windowLevelPresets
} = core_src.defaults;
/**
 *
 * @param {*} type - 'tool' | 'action' | 'toggle'
 * @param {*} id
 * @param {*} icon
 * @param {*} label
 */
function _createButton(type, id, icon, label, commands, tooltip) {
  return {
    id,
    icon,
    label,
    type,
    commands,
    tooltip
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
    }], 'Length Tool'), _createToolButton('Bidirectional', 'tool-bidirectional', 'Bidirectional', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'Bidirectional'
      },
      context: 'CORNERSTONE'
    }], 'Bidirectional Tool'), _createToolButton('EllipticalROI', 'tool-elipse', 'Ellipse', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'EllipticalROI'
      },
      context: 'CORNERSTONE'
    }], 'Ellipse Tool'), _createToolButton('CircleROI', 'tool-circle', 'Circle', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'CircleROI'
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
    renderer: src/* WindowLevelMenuItem */.eJ,
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
  type: 'ohif.layoutSelector'
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
    }], 'Flip Horizontal'), _createToolButton('StackScroll', 'tool-stack-scroll', 'Stack Scroll', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'StackScroll'
      },
      context: 'CORNERSTONE'
    }], 'Stack Scroll'), _createActionButton('invert', 'tool-invert', 'Invert', [{
      commandName: 'invertViewport',
      commandOptions: {},
      context: 'CORNERSTONE'
    }], 'Invert Colors'), _createToolButton('CalibrationLine', 'tool-calibration', 'Calibration', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'CalibrationLine'
      },
      context: 'CORNERSTONE'
    }], 'Calibration Line')]
  }
}];
/* harmony default export */ const src_toolbarButtons = (toolbarButtons);
;// CONCATENATED MODULE: ../../../modes/basic-dev-mode/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/mode-basic-dev-mode"}');
;// CONCATENATED MODULE: ../../../modes/basic-dev-mode/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../modes/basic-dev-mode/src/index.js



const configs = {
  Length: {}
  //
};

const ohif = {
  layout: '@ohif/extension-default.layoutTemplateModule.viewerLayout',
  sopClassHandler: '@ohif/extension-default.sopClassHandlerModule.stack',
  measurements: '@ohif/extension-default.panelModule.measure',
  thumbnailList: '@ohif/extension-default.panelModule.seriesList'
};
const cs3d = {
  viewport: '@ohif/extension-cornerstone.viewportModule.cornerstone'
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
const extensionDependencies = {
  '@ohif/extension-default': '^3.0.0',
  '@ohif/extension-cornerstone': '^3.0.0',
  '@ohif/extension-cornerstone-dicom-sr': '^3.0.0',
  '@ohif/extension-dicom-pdf': '^3.0.1',
  '@ohif/extension-dicom-video': '^3.0.1'
};
function modeFactory(_ref) {
  let {
    modeConfiguration
  } = _ref;
  return {
    id: id,
    routeName: 'dev',
    displayName: 'Basic Dev Viewer',
    /**
     * Lifecycle hooks
     */
    onModeEnter: _ref2 => {
      let {
        servicesManager,
        extensionManager
      } = _ref2;
      const {
        toolbarService,
        toolGroupService
      } = servicesManager.services;
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
          toolName: toolNames.Bidirectional
        }, {
          toolName: toolNames.Probe
        }, {
          toolName: toolNames.EllipticalROI
        }, {
          toolName: toolNames.CircleROI
        }, {
          toolName: toolNames.RectangleROI
        }, {
          toolName: toolNames.StackScroll
        }, {
          toolName: toolNames.CalibrationLine
        }],
        // enabled
        enabled: [{
          toolName: toolNames.ImageOverlayViewer
        }]
        // disabled
      };

      const toolGroupId = 'default';
      toolGroupService.createToolGroupAndAddTools(toolGroupId, tools);
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
      toolbarService.createButtonSection('primary', ['MeasurementTools', 'Zoom', 'WindowLevel', 'Pan', 'Layout', 'MoreTools']);
    },
    onModeExit: _ref3 => {
      let {
        servicesManager
      } = _ref3;
      const {
        toolGroupService,
        measurementService,
        toolbarService
      } = servicesManager.services;
      toolGroupService.destroy();
    },
    validationTags: {
      study: [],
      series: []
    },
    isValidMode: _ref4 => {
      let {
        modalities
      } = _ref4;
      const modalities_list = modalities.split('\\');

      // Slide Microscopy modality not supported by basic mode yet
      return !modalities_list.includes('SM');
    },
    routes: [{
      path: 'viewer-cs3d',
      /*init: ({ servicesManager, extensionManager }) => {
        //defaultViewerRouteInit
      },*/
      layoutTemplate: _ref5 => {
        let {
          location,
          servicesManager
        } = _ref5;
        return {
          id: ohif.layout,
          props: {
            // TODO: Should be optional, or required to pass empty array for slots?
            leftPanels: [ohif.thumbnailList],
            rightPanels: [ohif.measurements],
            viewports: [{
              namespace: cs3d.viewport,
              displaySetsToDisplay: [ohif.sopClassHandler]
            }, {
              namespace: dicomvideo.viewport,
              displaySetsToDisplay: [dicomvideo.sopClassHandler]
            }, {
              namespace: dicompdf.viewport,
              displaySetsToDisplay: [dicompdf.sopClassHandler]
            }]
          }
        };
      }
    }],
    extensions: extensionDependencies,
    hangingProtocol: 'default',
    sopClassHandlers: [dicomvideo.sopClassHandler, ohif.sopClassHandler, dicompdf.sopClassHandler, dicomsr.sopClassHandler],
    hotkeys: [...core_src/* hotkeys */.dD.defaults.hotkeyBindings]
  };
}
const mode = {
  id: id,
  modeFactory,
  extensionDependencies
};
/* harmony default export */ const basic_dev_mode_src = (mode);

/***/ })

}]);