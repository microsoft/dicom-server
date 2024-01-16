"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[687],{

/***/ 78687:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ src)
});

;// CONCATENATED MODULE: ../../../extensions/test-extension/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-test"}');
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../extensions/test-extension/src/hpTestSwitch.ts
const viewport0a = {
  viewportOptions: {
    viewportId: 'viewportA',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    id: 'defaultDisplaySetId'
  }]
};
const viewport1b = {
  viewportOptions: {
    viewportId: 'viewportB',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 1,
    id: 'defaultDisplaySetId'
  }]
};
const viewport2c = {
  viewportOptions: {
    viewportId: 'viewportC',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 2,
    id: 'defaultDisplaySetId'
  }]
};
const viewport3d = {
  viewportOptions: {
    viewportId: 'viewportD',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 3,
    id: 'defaultDisplaySetId'
  }]
};
const viewport4e = {
  viewportOptions: {
    viewportId: 'viewportE',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 4,
    id: 'defaultDisplaySetId'
  }]
};
const viewport5f = {
  viewportOptions: {
    viewportId: 'viewportF',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 5,
    id: 'defaultDisplaySetId'
  }]
};
const viewport3a = {
  viewportOptions: {
    viewportId: 'viewportA',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 3,
    id: 'defaultDisplaySetId'
  }]
};
const viewport2b = {
  viewportOptions: {
    viewportId: 'viewportB',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 2,
    id: 'defaultDisplaySetId'
  }]
};
const viewport1c = {
  viewportOptions: {
    viewportId: 'viewportC',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 1,
    id: 'defaultDisplaySetId'
  }]
};
const viewport0d = {
  viewportOptions: {
    viewportId: 'viewportD',
    toolGroupId: 'default',
    allowUnmatchedView: true
  },
  displaySets: [{
    matchedDisplaySetsIndex: 0,
    id: 'defaultDisplaySetId'
  }]
};
const viewportStructure = {
  layoutType: 'grid',
  properties: {
    rows: 2,
    columns: 2
  }
};
const viewportStructure32 = {
  layoutType: 'grid',
  properties: {
    rows: 2,
    columns: 3
  }
};

/**
 * This hanging protocol is a test hanging protocol used to apply various
 * layouts in different positions for display, re-using earlier names in
 * various orders.
 */
const hpTestSwitch = {
  hasUpdatedPriorsInformation: false,
  id: '@ohif/mnTestSwitch',
  description: 'Has various hanging protocol grid layouts',
  name: 'Test Switch',
  protocolMatchingRules: [{
    id: 'OneOrMoreSeries',
    weight: 25,
    attribute: 'numberOfDisplaySetsWithImages',
    constraint: {
      greaterThan: 0
    }
  }],
  toolGroupIds: ['default'],
  displaySetSelectors: {
    defaultDisplaySetId: {
      seriesMatchingRules: [{
        attribute: 'numImageFrames',
        constraint: {
          greaterThan: {
            value: 0
          }
        }
      },
      // This display set will select the specified items by preference
      // It has no affect if nothing is specified in the URL.
      {
        attribute: 'isDisplaySetFromUrl',
        weight: 10,
        constraint: {
          equals: true
        }
      }]
    }
  },
  defaultViewport: {
    viewportOptions: {
      viewportType: 'stack',
      toolGroupId: 'default',
      allowUnmatchedView: true
    },
    displaySets: [{
      id: 'defaultDisplaySetId',
      matchedDisplaySetsIndex: -1
    }]
  },
  stages: [{
    name: '2x2 0a1b2c3d',
    viewportStructure,
    viewports: [viewport0a, viewport1b, viewport2c, viewport3d]
  }, {
    name: '3x2 0a1b4e2c3d5f',
    viewportStructure: viewportStructure32,
    // Note the following structure simply preserves the viewportId for
    // a given screen position
    viewports: [viewport0a, viewport1b, viewport4e, viewport2c, viewport3d, viewport5f]
  }, {
    name: '2x2 1c0d3a2b',
    viewportStructure,
    viewports: [viewport1c, viewport0d, viewport3a, viewport2b]
  }, {
    name: '2x2 3a2b1c0d',
    viewportStructure,
    viewports: [viewport3a, viewport2b, viewport1c, viewport0d]
  }],
  numberOfPriorsReferenced: -1
};
/* harmony default export */ const src_hpTestSwitch = (hpTestSwitch);
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/custom-context-menu/codingValues.ts
/**
 * Coding values is a map of simple string coding values to a set of
 * attributes associated with the coding value.
 *
 * The simple string is in the format `<codingSchemeDesignator>:<codingValue>`
 * That allows extracting the DICOM attributes from the designator/value, and
 * allows for passing around the simple string.
 * The additional attributes contained in the object include:
 *       * text - this is the coding scheme text display value, and may be language specific
 *       * type - this defines a named type, typically 'site'.  Different names can be used
 *                to allow setting different findingSites values in order to define a hierarchy.
 *       * color - used to apply annotation color
 * It is also possible to define additional attributes here, used by custom
 * extensions.
 *
 * See https://dicom.nema.org/medical/dicom/current/output/html/part16.html
 * for definitions of SCT and other code values.
 */
const codingValues = {
  id: 'codingValues',
  // Sites
  'SCT:69536005': {
    text: 'Head',
    type: 'site'
  },
  'SCT:45048000': {
    text: 'Neck',
    type: 'site'
  },
  'SCT:818981001': {
    text: 'Abdomen',
    type: 'site'
  },
  'SCT:816092008': {
    text: 'Pelvis',
    type: 'site'
  },
  // Findings
  'SCT:371861004': {
    text: 'Mild intimal coronary irregularities',
    color: 'green'
  },
  'SCT:194983005': {
    text: 'Aortic insufficiency',
    color: 'darkred'
  },
  'SCT:399232001': {
    text: '2-chamber'
  },
  'SCT:103340004': {
    text: 'SAX'
  },
  'SCT:91134007': {
    text: 'MV'
  },
  'SCT:122972007': {
    text: 'PV'
  },
  // Orientations
  'SCT:24422004': {
    text: 'Axial',
    color: '#000000',
    type: 'orientation'
  },
  'SCT:81654009': {
    text: 'Coronal',
    color: '#000000',
    type: 'orientation'
  },
  'SCT:30730003': {
    text: 'Sagittal',
    color: '#000000',
    type: 'orientation'
  }
};
/* harmony default export */ const custom_context_menu_codingValues = (codingValues);
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/custom-context-menu/contextMenuCodeItem.ts
const codeMenuItem = {
  id: '@ohif/contextMenuAnnotationCode',
  /** Applies the code value setup for this item */
  transform: function (customizationService) {
    const {
      code: codeRef
    } = this;
    if (!codeRef) {
      throw new Error(`item ${this} has no code ref`);
    }
    const codingValues = customizationService.get('codingValues');
    const code = codingValues[codeRef];
    return {
      ...this,
      codeRef,
      code: {
        ref: codeRef,
        ...code
      },
      label: code.text,
      commands: [{
        commandName: 'updateMeasurement'
      }]
    };
  }
};
/* harmony default export */ const contextMenuCodeItem = (codeMenuItem);
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/custom-context-menu/findingsContextMenu.ts
const findingsContextMenu = {
  id: 'measurementsContextMenu',
  customizationType: 'ohif.contextMenu',
  menus: [{
    id: 'forExistingMeasurement',
    // selector restricts context menu to when there is nearbyToolData
    selector: _ref => {
      let {
        nearbyToolData
      } = _ref;
      return !!nearbyToolData;
    },
    items: [{
      customizationType: 'ohif.contextSubMenu',
      label: 'Site',
      actionType: 'ShowSubMenu',
      subMenu: 'siteSelectionSubMenu'
    }, {
      customizationType: 'ohif.contextSubMenu',
      label: 'Finding',
      actionType: 'ShowSubMenu',
      subMenu: 'findingSelectionSubMenu'
    }, {
      // customizationType is implicit here in the configuration setup
      label: 'Delete Measurement',
      commands: [{
        commandName: 'deleteMeasurement'
      }]
    }, {
      label: 'Add Label',
      commands: [{
        commandName: 'setMeasurementLabel'
      }]
    },
    // The example below shows how to include a delegating sub-menu,
    // Only available on the @ohif/mnGrid hanging protocol
    // To demonstrate, select the 3x1 layout from the protocol menu
    // and right click on a measurement.
    {
      label: 'IncludeSubMenu',
      selector: _ref2 => {
        let {
          protocol
        } = _ref2;
        return protocol?.id === '@ohif/mnGrid';
      },
      delegating: true,
      subMenu: 'orientationSelectionSubMenu'
    }]
  }, {
    id: 'orientationSelectionSubMenu',
    selector: _ref3 => {
      let {
        nearbyToolData
      } = _ref3;
      return false;
    },
    items: [{
      customizationType: '@ohif/contextMenuAnnotationCode',
      code: 'SCT:24422004'
    }, {
      customizationType: '@ohif/contextMenuAnnotationCode',
      code: 'SCT:81654009'
    }]
  }, {
    id: 'findingSelectionSubMenu',
    selector: _ref4 => {
      let {
        nearbyToolData
      } = _ref4;
      return false;
    },
    items: [{
      customizationType: '@ohif/contextMenuAnnotationCode',
      code: 'SCT:371861004'
    }, {
      customizationType: '@ohif/contextMenuAnnotationCode',
      code: 'SCT:194983005'
    }]
  }, {
    id: 'siteSelectionSubMenu',
    selector: _ref5 => {
      let {
        nearbyToolData
      } = _ref5;
      return !!nearbyToolData;
    },
    items: [{
      customizationType: '@ohif/contextMenuAnnotationCode',
      code: 'SCT:69536005'
    }, {
      customizationType: '@ohif/contextMenuAnnotationCode',
      code: 'SCT:45048000'
    }]
  }]
};
/* harmony default export */ const custom_context_menu_findingsContextMenu = (findingsContextMenu);
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/custom-context-menu/index.ts




;// CONCATENATED MODULE: ../../../extensions/test-extension/src/getCustomizationModule.ts

function getCustomizationModule() {
  return [{
    name: 'custom-context-menu',
    value: [custom_context_menu_codingValues, contextMenuCodeItem, custom_context_menu_findingsContextMenu]
  }];
}
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/custom-attribute/sameAs.ts
/**
 * This function extracts an attribute from the already matched display sets, and
 * compares it to the attribute in the current display set, and indicates if they match.
 * From 'this', it uses:
 *    `sameAttribute` as the attribute name to look for
 *    `sameDisplaySetId` as the display set id to look for
 * From `options`, it looks for
 */
/* harmony default export */ function sameAs(displaySet, options) {
  const {
    sameAttribute,
    sameDisplaySetId
  } = this;
  if (!sameAttribute) {
    console.log('sameAttribute not defined in', this);
    return `sameAttribute not defined in ${this.id}`;
  }
  if (!sameDisplaySetId) {
    console.log('sameDisplaySetId not defined in', this);
    return `sameDisplaySetId not defined in ${this.id}`;
  }
  const {
    displaySetMatchDetails,
    displaySets
  } = options;
  const match = displaySetMatchDetails.get(sameDisplaySetId);
  if (!match) {
    console.log('No match for display set', sameDisplaySetId);
    return false;
  }
  const {
    displaySetInstanceUID
  } = match;
  const altDisplaySet = displaySets.find(it => it.displaySetInstanceUID == displaySetInstanceUID);
  if (!altDisplaySet) {
    console.log('No display set found with', displaySetInstanceUID, 'in', displaySets);
    return false;
  }
  const testValue = altDisplaySet[sameAttribute];
  return testValue === displaySet[sameAttribute];
}
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/custom-attribute/numberOfDisplaySets.ts
/* harmony default export */ const numberOfDisplaySets = ((study, extraData) => extraData?.displaySets?.length);
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/custom-attribute/maxNumImageFrames.ts
/* harmony default export */ const maxNumImageFrames = ((study, extraData) => Math.max(...(extraData?.displaySets?.map?.(ds => ds.numImageFrames ?? 0) || [0])));
;// CONCATENATED MODULE: ../../../extensions/test-extension/src/index.tsx



// import {setViewportZoomPan, storeViewportZoomPan } from './custom-viewport/setViewportZoomPan';




/**
 * The test extension provides additional behaviour for testing various
 * customizations and settings for OHIF.
 */
const testExtension = {
  /**
   * Only required property. Should be a unique value across all extensions.
   */
  id: id,
  /** Register additional behaviour:
   *   * HP custom attribute seriesDescriptions to retrieve an array of all series descriptions
   *   * HP custom attribute numberOfDisplaySets to retrieve the number of display sets
   *   * HP custom attribute numberOfDisplaySetsWithImages to retrieve the number of display sets containing images
   *   * HP custom attribute to return a boolean true, when the attribute sameAttribute has the same
   *     value as another series description in an already matched display set selector named with the value
   *     in `sameDisplaySetId`
   */
  preRegistration: _ref => {
    let {
      servicesManager
    } = _ref;
    const {
      hangingProtocolService
    } = servicesManager.services;
    hangingProtocolService.addCustomAttribute('numberOfDisplaySets', 'Number of displays sets', numberOfDisplaySets);
    hangingProtocolService.addCustomAttribute('maxNumImageFrames', 'Maximum of number of image frames', maxNumImageFrames);
    hangingProtocolService.addCustomAttribute('sameAs', 'Match an attribute in an existing display set', sameAs);
  },
  /** Registers some customizations */
  getCustomizationModule: getCustomizationModule,
  getHangingProtocolModule: () => {
    return [
    // Create a MxN hanging protocol available by default
    {
      name: src_hpTestSwitch.id,
      protocol: src_hpTestSwitch
    }];
  }
};
/* harmony default export */ const src = (testExtension);

/***/ })

}]);