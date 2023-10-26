"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[281],{

/***/ 42281:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  cornerstone: () => (/* binding */ cornerstone),
  "default": () => (/* binding */ microscopy_src)
});

// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
;// CONCATENATED MODULE: ../../../modes/microscopy/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/mode-microscopy"}');
;// CONCATENATED MODULE: ../../../modes/microscopy/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../modes/microscopy/src/toolbarButtons.js
// TODO: torn, can either bake this here; or have to create a whole new button type
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
    primary: _createToolButton('line', 'tool-length', 'Line', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'line'
      },
      context: 'MICROSCOPY'
    }], 'Line'),
    secondary: {
      icon: 'chevron-down',
      label: '',
      isActive: true,
      tooltip: 'More Measure Tools'
    },
    items: [_createToolButton('line', 'tool-length', 'Line', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'line'
      },
      context: 'MICROSCOPY'
    }], 'Line Tool'), _createToolButton('point', 'tool-point', 'Point', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'point'
      },
      context: 'MICROSCOPY'
    }], 'Point Tool'), _createToolButton('polygon', 'tool-polygon', 'Polygon', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'polygon'
      },
      context: 'MICROSCOPY'
    }], 'Polygon Tool'), _createToolButton('circle', 'tool-circle', 'Circle', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'circle'
      },
      context: 'MICROSCOPY'
    }], 'Circle Tool'), _createToolButton('box', 'tool-rectangle', 'Box', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'box'
      },
      context: 'MICROSCOPY'
    }], 'Box Tool'), _createToolButton('freehandpolygon', 'tool-freehand-polygon', 'Freehand Polygon', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'freehandpolygon'
      },
      context: 'MICROSCOPY'
    }], 'Freehand Polygon Tool'), _createToolButton('freehandline', 'tool-freehand-line', 'Freehand Line', [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'freehandline'
      },
      context: 'MICROSCOPY'
    }], 'Freehand Line Tool')]
  }
},
// Pan...
{
  id: 'dragPan',
  type: 'ohif.radioGroup',
  props: {
    type: 'tool',
    icon: 'tool-move',
    label: 'Pan',
    commands: [{
      commandName: 'setToolActive',
      commandOptions: {
        toolName: 'dragPan'
      },
      context: 'MICROSCOPY'
    }]
  }
}];
/* harmony default export */ const src_toolbarButtons = (toolbarButtons);
;// CONCATENATED MODULE: ../../../modes/microscopy/src/index.tsx



const ohif = {
  layout: '@ohif/extension-default.layoutTemplateModule.viewerLayout',
  sopClassHandler: '@ohif/extension-default.sopClassHandlerModule.stack',
  hangingProtocols: '@ohif/extension-default.hangingProtocolModule.default',
  leftPanel: '@ohif/extension-default.panelModule.seriesList',
  rightPanel: '@ohif/extension-default.panelModule.measure'
};
const cornerstone = {
  viewport: '@ohif/extension-cornerstone.viewportModule.cornerstone'
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
  // Can derive the versions at least process.env.from npm_package_version
  '@ohif/extension-default': '^3.0.0',
  '@ohif/extension-cornerstone': '^3.0.0',
  '@ohif/extension-cornerstone-dicom-sr': '^3.0.0',
  '@ohif/extension-dicom-pdf': '^3.0.1',
  '@ohif/extension-dicom-video': '^3.0.1',
  '@ohif/extension-dicom-microscopy': '^3.0.0'
};
function modeFactory(_ref) {
  let {
    modeConfiguration
  } = _ref;
  return {
    // TODO: We're using this as a route segment
    // We should not be.
    id: id,
    routeName: 'microscopy',
    displayName: 'Microscopy',
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
        toolbarService
      } = servicesManager.services;
      toolbarService.init(extensionManager);
      toolbarService.addButtons(src_toolbarButtons);
      toolbarService.createButtonSection('primary', ['MeasurementTools', 'dragPan']);
    },
    onModeExit: _ref3 => {
      let {
        servicesManager
      } = _ref3;
      const {
        toolbarService
      } = servicesManager.services;
      toolbarService.reset();
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

      // Slide Microscopy and ECG modality not supported by basic mode yet
      return modalities_list.includes('SM');
    },
    routes: [{
      path: 'microscopy',
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
            leftPanels: [ohif.leftPanel],
            leftPanelDefaultClosed: true,
            // we have problem with rendering thumbnails for microscopy images
            rightPanelDefaultClosed: true,
            // we do not have the save microscopy measurements yet
            rightPanels: ['@ohif/extension-dicom-microscopy.panelModule.measure'],
            viewports: [{
              namespace: '@ohif/extension-dicom-microscopy.viewportModule.microscopy-dicom',
              displaySetsToDisplay: ['@ohif/extension-dicom-microscopy.sopClassHandlerModule.DicomMicroscopySopClassHandler', '@ohif/extension-dicom-microscopy.sopClassHandlerModule.DicomMicroscopySRSopClassHandler']
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
    hangingProtocols: [ohif.hangingProtocols],
    hangingProtocol: ['default'],
    // Order is important in sop class handlers when two handlers both use
    // the same sop class under different situations.  In that case, the more
    // general handler needs to come last.  For this case, the dicomvideo must
    // come first to remove video transfer syntax before ohif uses images
    sopClassHandlers: ['@ohif/extension-dicom-microscopy.sopClassHandlerModule.DicomMicroscopySopClassHandler', '@ohif/extension-dicom-microscopy.sopClassHandlerModule.DicomMicroscopySRSopClassHandler', dicomvideo.sopClassHandler, dicompdf.sopClassHandler],
    hotkeys: [...src/* hotkeys */.dD.defaults.hotkeyBindings],
    ...modeConfiguration
  };
}
const mode = {
  id: id,
  modeFactory,
  extensionDependencies
};
/* harmony default export */ const microscopy_src = (mode);

/***/ })

}]);