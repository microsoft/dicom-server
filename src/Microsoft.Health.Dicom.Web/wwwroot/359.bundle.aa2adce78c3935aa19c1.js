"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[359],{

/***/ 22359:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ tmtv_src)
});

// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var ui_src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../modes/tmtv/src/initToolGroups.js
const toolGroupIds = {
  CT: 'ctToolGroup',
  PT: 'ptToolGroup',
  Fusion: 'fusionToolGroup',
  MIP: 'mipToolGroup',
  default: 'default'
  // MPR: 'mpr',
};

function _initToolGroups(toolNames, Enums, toolGroupService, commandsManager) {
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
        getTextCallback: (callback, eventDetails) => {
          commandsManager.runCommand('arrowTextCallback', {
            callback,
            eventDetails
          });
        },
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
      toolName: toolNames.Probe
    }, {
      toolName: toolNames.EllipticalROI
    }, {
      toolName: toolNames.RectangleROI
    }, {
      toolName: toolNames.StackScroll
    }, {
      toolName: toolNames.Angle
    }, {
      toolName: toolNames.CobbAngle
    }, {
      toolName: toolNames.Magnify
    }],
    enabled: [{
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
    }]
  };
  toolGroupService.createToolGroupAndAddTools(toolGroupIds.CT, tools);
  toolGroupService.createToolGroupAndAddTools(toolGroupIds.PT, {
    active: tools.active,
    passive: [...tools.passive, {
      toolName: 'RectangleROIStartEndThreshold'
    }],
    enabled: tools.enabled,
    disabled: tools.disabled
  });
  toolGroupService.createToolGroupAndAddTools(toolGroupIds.Fusion, tools);
  toolGroupService.createToolGroupAndAddTools(toolGroupIds.default, tools);
  const mipTools = {
    active: [{
      toolName: toolNames.VolumeRotateMouseWheel,
      configuration: {
        rotateIncrementDegrees: 0.1
      }
    }, {
      toolName: toolNames.MipJumpToClick,
      configuration: {
        toolGroupId: toolGroupIds.PT
      },
      bindings: [{
        mouseButton: Enums.MouseBindings.Primary
      }]
    }],
    enabled: [{
      toolName: toolNames.SegmentationDisplay
    }]
  };
  toolGroupService.createToolGroupAndAddTools(toolGroupIds.MIP, mipTools);
}
function initToolGroups(toolNames, Enums, toolGroupService, commandsManager) {
  _initToolGroups(toolNames, Enums, toolGroupService, commandsManager);
}
/* harmony default export */ const src_initToolGroups = (initToolGroups);
;// CONCATENATED MODULE: ../../../modes/tmtv/src/toolbarButtons.js
// TODO: torn, can either bake this here; or have to create a whole new button type
// Only ways that you can pass in a custom React component for render :l



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
function _createColormap(label, colormap) {
  return {
    id: label,
    label,
    type: 'action',
    commands: [{
      commandName: 'setFusionPTColormap',
      commandOptions: {
        toolGroupId: toolGroupIds.Fusion,
        colormap
      }
    }]
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
function _createCommands(commandName, toolName, toolGroupIds) {
  return toolGroupIds.map(toolGroupId => ({
    /* It's a command that is being run when the button is clicked. */
    commandName,
    commandOptions: {
      toolName,
      toolGroupId
    },
    context: 'CORNERSTONE'
  }));
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
    primary: _createToolButton('Length', 'tool-length', 'Length', [..._createCommands('setToolActive', 'Length', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])], 'Length'),
    secondary: {
      icon: 'chevron-down',
      label: '',
      isActive: true,
      tooltip: 'More Measure Tools'
    },
    items: [_createToolButton('Length', 'tool-length', 'Length', [..._createCommands('setToolActive', 'Length', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])], 'Length Tool'), _createToolButton('Bidirectional', 'tool-bidirectional', 'Bidirectional', [..._createCommands('setToolActive', 'Bidirectional', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])], 'Bidirectional Tool'), _createToolButton('ArrowAnnotate', 'tool-annotate', 'Annotation', [..._createCommands('setToolActive', 'ArrowAnnotate', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])], 'Arrow Annotate'), _createToolButton('EllipticalROI', 'tool-elipse', 'Ellipse', [..._createCommands('setToolActive', 'EllipticalROI', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])], 'Ellipse Tool')]
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
    commands: [..._createCommands('setToolActive', 'Zoom', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])]
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
},
// Window Level + Presets...
{
  id: 'WindowLevel',
  type: 'ohif.splitButton',
  props: {
    groupId: 'WindowLevel',
    primary: _createToolButton('WindowLevel', 'tool-window-level', 'Window Level', [..._createCommands('setToolActive', 'WindowLevel', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])], 'Window Level'),
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
}, {
  id: 'Crosshairs',
  type: 'ohif.radioGroup',
  props: {
    type: 'tool',
    icon: 'tool-crosshair',
    label: 'Crosshairs',
    commands: [..._createCommands('setToolActive', 'Crosshairs', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])]
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
    commands: [..._createCommands('setToolActive', 'Pan', [toolGroupIds.CT, toolGroupIds.PT, toolGroupIds.Fusion
    // toolGroupIds.MPR,
    ])]
  }
}, {
  id: 'RectangleROIStartEndThreshold',
  type: 'ohif.radioGroup',
  props: {
    type: 'tool',
    icon: 'tool-create-threshold',
    label: 'Rectangle ROI Threshold',
    commands: [..._createCommands('setToolActive', 'RectangleROIStartEndThreshold', [toolGroupIds.PT]), {
      commandName: 'displayNotification',
      commandOptions: {
        title: 'RectangleROI Threshold Tip',
        text: 'RectangleROI Threshold tool should be used on PT Axial Viewport',
        type: 'info'
      }
    }, {
      commandName: 'setViewportActive',
      commandOptions: {
        viewportId: 'ptAXIAL'
      }
    }]
  }
}, {
  id: 'fusionPTColormap',
  type: 'ohif.splitButton',
  props: {
    groupId: 'fusionPTColormap',
    primary: _createToolButton('fusionPTColormap', 'tool-fusion-color', 'Fusion PT Colormap', [], 'Fusion PT Colormap'),
    secondary: {
      icon: 'chevron-down',
      label: 'PT Colormap',
      isActive: true,
      tooltip: 'PET Image Colormap'
    },
    isAction: true,
    // ?
    items: [_createColormap('HSV', 'hsv'), _createColormap('Hot Iron', 'hot_iron'), _createColormap('S PET', 's_pet'), _createColormap('Red Hot', 'red_hot'), _createColormap('Perfusion', 'perfusion'), _createColormap('Rainbow', 'rainbow_2'), _createColormap('SUV', 'suv'), _createColormap('GE 256', 'ge_256'), _createColormap('GE', 'ge'), _createColormap('Siemens', 'siemens')]
  }
}];
/* harmony default export */ const src_toolbarButtons = (toolbarButtons);
;// CONCATENATED MODULE: ../../../modes/tmtv/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/mode-tmtv"}');
;// CONCATENATED MODULE: ../../../modes/tmtv/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../modes/tmtv/src/utils/setCrosshairsConfiguration.js

function setCrosshairsConfiguration(matches, toolNames, toolGroupService, displaySetService) {
  const matchDetails = matches.get('ctDisplaySet');
  if (!matchDetails) {
    return;
  }
  const {
    SeriesInstanceUID
  } = matchDetails;
  const displaySets = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
  const toolConfig = toolGroupService.getToolConfiguration(toolGroupIds.Fusion, toolNames.Crosshairs);
  const crosshairsConfig = {
    ...toolConfig,
    filterActorUIDsToSetSlabThickness: [displaySets[0].displaySetInstanceUID]
  };
  toolGroupService.setToolConfiguration(toolGroupIds.Fusion, toolNames.Crosshairs, crosshairsConfig);
}
;// CONCATENATED MODULE: ../../../modes/tmtv/src/utils/setFusionActiveVolume.js

function setFusionActiveVolume(matches, toolNames, toolGroupService, displaySetService) {
  const matchDetails = matches.get('ptDisplaySet');
  if (!matchDetails) {
    return;
  }
  const {
    SeriesInstanceUID
  } = matchDetails;
  const displaySets = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
  if (!displaySets || displaySets.length === 0) {
    return;
  }
  const wlToolConfig = toolGroupService.getToolConfiguration(toolGroupIds.Fusion, toolNames.WindowLevel);
  const ellipticalToolConfig = toolGroupService.getToolConfiguration(toolGroupIds.Fusion, toolNames.EllipticalROI);

  // Todo: this should not take into account the loader id
  const volumeId = `cornerstoneStreamingImageVolume:${displaySets[0].displaySetInstanceUID}`;
  const windowLevelConfig = {
    ...wlToolConfig,
    volumeId
  };
  const ellipticalROIConfig = {
    ...ellipticalToolConfig,
    volumeId
  };
  toolGroupService.setToolConfiguration(toolGroupIds.Fusion, toolNames.WindowLevel, windowLevelConfig);
  toolGroupService.setToolConfiguration(toolGroupIds.Fusion, toolNames.EllipticalROI, ellipticalROIConfig);
}
;// CONCATENATED MODULE: ../../../modes/tmtv/src/index.js






const {
  MetadataProvider
} = src.classes;
const ohif = {
  layout: '@ohif/extension-default.layoutTemplateModule.viewerLayout',
  sopClassHandler: '@ohif/extension-default.sopClassHandlerModule.stack',
  measurements: '@ohif/extension-default.panelModule.measure',
  thumbnailList: '@ohif/extension-default.panelModule.seriesList'
};
const cs3d = {
  viewport: '@ohif/extension-cornerstone.viewportModule.cornerstone'
};
const tmtv = {
  hangingProtocol: '@ohif/extension-tmtv.hangingProtocolModule.ptCT',
  petSUV: '@ohif/extension-tmtv.panelModule.petSUV',
  ROIThresholdPanel: '@ohif/extension-tmtv.panelModule.ROIThresholdSeg'
};
const extensionDependencies = {
  // Can derive the versions at least process.env.from npm_package_version
  '@ohif/extension-default': '^3.0.0',
  '@ohif/extension-cornerstone': '^3.0.0',
  '@ohif/extension-tmtv': '^3.0.0'
};
let unsubscriptions = [];
function modeFactory(_ref) {
  let {
    modeConfiguration
  } = _ref;
  return {
    // TODO: We're using this as a route segment
    // We should not be.
    id: id,
    routeName: 'tmtv',
    displayName: 'Total Metabolic Tumor Volume',
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
        toolbarService,
        toolGroupService,
        hangingProtocolService,
        displaySetService
      } = servicesManager.services;
      const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.tools');
      const {
        toolNames,
        Enums
      } = utilityModule.exports;

      // Init Default and SR ToolGroups
      src_initToolGroups(toolNames, Enums, toolGroupService, commandsManager);
      const setWindowLevelActive = () => {
        toolbarService.recordInteraction({
          groupId: 'WindowLevel',
          interactionType: 'tool',
          commands: [{
            commandName: 'setToolActive',
            commandOptions: {
              toolName: toolNames.WindowLevel,
              toolGroupId: toolGroupIds.CT
            },
            context: 'CORNERSTONE'
          }, {
            commandName: 'setToolActive',
            commandOptions: {
              toolName: toolNames.WindowLevel,
              toolGroupId: toolGroupIds.PT
            },
            context: 'CORNERSTONE'
          }, {
            commandName: 'setToolActive',
            commandOptions: {
              toolName: toolNames.WindowLevel,
              toolGroupId: toolGroupIds.Fusion
            },
            context: 'CORNERSTONE'
          }]
        });
      };
      const {
        unsubscribe
      } = toolGroupService.subscribe(toolGroupService.EVENTS.VIEWPORT_ADDED, () => {
        // For fusion toolGroup we need to add the volumeIds for the crosshairs
        // since in the fusion viewport we don't want both PT and CT to render MIP
        // when slabThickness is modified
        const {
          displaySetMatchDetails
        } = hangingProtocolService.getMatchDetails();
        setCrosshairsConfiguration(displaySetMatchDetails, toolNames, toolGroupService, displaySetService);
        setFusionActiveVolume(displaySetMatchDetails, toolNames, toolGroupService, displaySetService);
        setWindowLevelActive();
      });
      unsubscriptions.push(unsubscribe);
      toolbarService.init(extensionManager);
      toolbarService.addButtons(src_toolbarButtons);
      toolbarService.createButtonSection('primary', ['MeasurementTools', 'Zoom', 'WindowLevel', 'Crosshairs', 'Pan', 'RectangleROIStartEndThreshold', 'fusionPTColormap']);

      // For the hanging protocol we need to decide on the window level
      // based on whether the SUV is corrected or not, hence we can't hard
      // code the window level in the hanging protocol but we add a custom
      // attribute to the hanging protocol that will be used to get the
      // window level based on the metadata
      hangingProtocolService.addCustomAttribute('getPTVOIRange', 'get PT VOI based on corrected or not', props => {
        const ptDisplaySet = props.find(imageSet => imageSet.Modality === 'PT');
        if (!ptDisplaySet) {
          return;
        }
        const {
          imageId
        } = ptDisplaySet.images[0];
        const imageIdScalingFactor = MetadataProvider.get('scalingModule', imageId);
        const isSUVAvailable = imageIdScalingFactor && imageIdScalingFactor.suvbw;
        if (isSUVAvailable) {
          return {
            windowWidth: 5,
            windowCenter: 2.5
          };
        }
        return;
      });
    },
    onModeExit: _ref3 => {
      let {
        servicesManager
      } = _ref3;
      const {
        toolGroupService,
        syncGroupService,
        segmentationService,
        cornerstoneViewportService
      } = servicesManager.services;
      unsubscriptions.forEach(unsubscribe => unsubscribe());
      toolGroupService.destroy();
      syncGroupService.destroy();
      segmentationService.destroy();
      cornerstoneViewportService.destroy();
    },
    validationTags: {
      study: [],
      series: []
    },
    isValidMode: _ref4 => {
      let {
        modalities,
        study
      } = _ref4;
      const modalities_list = modalities.split('\\');
      const invalidModalities = ['SM'];
      const isValid = modalities_list.includes('CT') && modalities_list.includes('PT') && !invalidModalities.some(modality => modalities_list.includes(modality)) &&
      // This is study is a 4D study with PT and CT and not a 3D study for the tmtv
      // mode, until we have a better way to identify 4D studies we will use the
      // StudyInstanceUID to identify the study
      // Todo: when we add the 4D mode which comes with a mechanism to identify
      // 4D studies we can use that
      study.studyInstanceUid !== '1.3.6.1.4.1.12842.1.1.14.3.20220915.105557.468.2963630849';

      // there should be both CT and PT modalities and the modality should not be SM
      return isValid;
    },
    routes: [{
      path: 'tmtv',
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
            leftPanels: [ohif.thumbnailList],
            leftPanelDefaultClosed: true,
            rightPanels: [tmtv.ROIThresholdPanel, tmtv.petSUV],
            viewports: [{
              namespace: cs3d.viewport,
              displaySetsToDisplay: [ohif.sopClassHandler]
            }]
          }
        };
      }
    }],
    extensions: extensionDependencies,
    hangingProtocol: tmtv.hangingProtocol,
    sopClassHandlers: [ohif.sopClassHandler],
    hotkeys: [...src/* hotkeys */.dD.defaults.hotkeyBindings],
    ...modeConfiguration
  };
}
const mode = {
  id: id,
  modeFactory,
  extensionDependencies
};
/* harmony default export */ const tmtv_src = (mode);

/***/ })

}]);