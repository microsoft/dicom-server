(self["webpackChunk"] = self["webpackChunk"] || []).push([[788],{

/***/ 4483:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ tmtv_src)
});

;// CONCATENATED MODULE: ../../../extensions/tmtv/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-tmtv"}');
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/id.js

const id = package_namespaceObject.u2;

;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/hpViewports.ts
const ctAXIAL = {
  viewportOptions: {
    viewportId: 'ctAXIAL',
    viewportType: 'volume',
    orientation: 'axial',
    toolGroupId: 'ctToolGroup',
    initialImageOptions: {
      // index: 5,
      preset: 'first' // 'first', 'last', 'middle'
    },

    syncGroups: [{
      type: 'cameraPosition',
      id: 'axialSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ctWLSync',
      source: true,
      target: true
    }]
  },
  displaySets: [{
    id: 'ctDisplaySet'
  }]
};
const ctSAGITTAL = {
  viewportOptions: {
    viewportId: 'ctSAGITTAL',
    viewportType: 'volume',
    orientation: 'sagittal',
    toolGroupId: 'ctToolGroup',
    syncGroups: [{
      type: 'cameraPosition',
      id: 'sagittalSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ctWLSync',
      source: true,
      target: true
    }]
  },
  displaySets: [{
    id: 'ctDisplaySet'
  }]
};
const ctCORONAL = {
  viewportOptions: {
    viewportId: 'ctCORONAL',
    viewportType: 'volume',
    orientation: 'coronal',
    toolGroupId: 'ctToolGroup',
    syncGroups: [{
      type: 'cameraPosition',
      id: 'coronalSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ctWLSync',
      source: true,
      target: true
    }]
  },
  displaySets: [{
    id: 'ctDisplaySet'
  }]
};
const ptAXIAL = {
  viewportOptions: {
    viewportId: 'ptAXIAL',
    viewportType: 'volume',
    background: [1, 1, 1],
    orientation: 'axial',
    toolGroupId: 'ptToolGroup',
    initialImageOptions: {
      // index: 5,
      preset: 'first' // 'first', 'last', 'middle'
    },

    syncGroups: [{
      type: 'cameraPosition',
      id: 'axialSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptWLSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptFusionWLSync',
      source: true,
      target: false,
      options: {
        syncInvertState: false
      }
    }]
  },
  displaySets: [{
    options: {
      voi: {
        custom: 'getPTVOIRange'
      },
      voiInverted: true
    },
    id: 'ptDisplaySet'
  }]
};
const ptSAGITTAL = {
  viewportOptions: {
    viewportId: 'ptSAGITTAL',
    viewportType: 'volume',
    orientation: 'sagittal',
    background: [1, 1, 1],
    toolGroupId: 'ptToolGroup',
    syncGroups: [{
      type: 'cameraPosition',
      id: 'sagittalSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptWLSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptFusionWLSync',
      source: true,
      target: false,
      options: {
        syncInvertState: false
      }
    }]
  },
  displaySets: [{
    options: {
      voi: {
        custom: 'getPTVOIRange'
      },
      voiInverted: true
    },
    id: 'ptDisplaySet'
  }]
};
const ptCORONAL = {
  viewportOptions: {
    viewportId: 'ptCORONAL',
    viewportType: 'volume',
    orientation: 'coronal',
    background: [1, 1, 1],
    toolGroupId: 'ptToolGroup',
    syncGroups: [{
      type: 'cameraPosition',
      id: 'coronalSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptWLSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptFusionWLSync',
      source: true,
      target: false,
      options: {
        syncInvertState: false
      }
    }]
  },
  displaySets: [{
    options: {
      voi: {
        custom: 'getPTVOIRange'
      },
      voiInverted: true
    },
    id: 'ptDisplaySet'
  }]
};
const fusionAXIAL = {
  viewportOptions: {
    viewportId: 'fusionAXIAL',
    viewportType: 'volume',
    orientation: 'axial',
    toolGroupId: 'fusionToolGroup',
    initialImageOptions: {
      // index: 5,
      preset: 'first' // 'first', 'last', 'middle'
    },

    syncGroups: [{
      type: 'cameraPosition',
      id: 'axialSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ctWLSync',
      source: false,
      target: true
    }, {
      type: 'voi',
      id: 'fusionWLSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptFusionWLSync',
      source: false,
      target: true,
      options: {
        syncInvertState: false
      }
    }]
  },
  displaySets: [{
    id: 'ctDisplaySet'
  }, {
    id: 'ptDisplaySet',
    options: {
      colormap: {
        name: 'hsv',
        opacity: [{
          value: 0,
          opacity: 0
        }, {
          value: 0.1,
          opacity: 0.9
        }, {
          value: 1,
          opacity: 0.95
        }]
      },
      voi: {
        custom: 'getPTVOIRange'
      }
    }
  }]
};
const fusionSAGITTAL = {
  viewportOptions: {
    viewportId: 'fusionSAGITTAL',
    viewportType: 'volume',
    orientation: 'sagittal',
    toolGroupId: 'fusionToolGroup',
    // initialImageOptions: {
    //   index: 180,
    //   preset: 'middle', // 'first', 'last', 'middle'
    // },
    syncGroups: [{
      type: 'cameraPosition',
      id: 'sagittalSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ctWLSync',
      source: false,
      target: true
    }, {
      type: 'voi',
      id: 'fusionWLSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptFusionWLSync',
      source: false,
      target: true,
      options: {
        syncInvertState: false
      }
    }]
  },
  displaySets: [{
    id: 'ctDisplaySet'
  }, {
    id: 'ptDisplaySet',
    options: {
      colormap: {
        name: 'hsv',
        opacity: [{
          value: 0,
          opacity: 0
        }, {
          value: 0.1,
          opacity: 0.9
        }, {
          value: 1,
          opacity: 0.95
        }]
      },
      voi: {
        custom: 'getPTVOIRange'
      }
    }
  }]
};
const fusionCORONAL = {
  viewportOptions: {
    viewportId: 'fusionCoronal',
    viewportType: 'volume',
    orientation: 'coronal',
    toolGroupId: 'fusionToolGroup',
    // initialImageOptions: {
    //   index: 180,
    //   preset: 'middle', // 'first', 'last', 'middle'
    // },
    syncGroups: [{
      type: 'cameraPosition',
      id: 'coronalSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ctWLSync',
      source: false,
      target: true
    }, {
      type: 'voi',
      id: 'fusionWLSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptFusionWLSync',
      source: false,
      target: true,
      options: {
        syncInvertState: false
      }
    }]
  },
  displaySets: [{
    id: 'ctDisplaySet'
  }, {
    id: 'ptDisplaySet',
    options: {
      colormap: {
        name: 'hsv',
        opacity: [{
          value: 0,
          opacity: 0
        }, {
          value: 0.1,
          opacity: 0.9
        }, {
          value: 1,
          opacity: 0.95
        }]
      },
      voi: {
        custom: 'getPTVOIRange'
      }
    }
  }]
};
const mipSAGITTAL = {
  viewportOptions: {
    viewportId: 'mipSagittal',
    viewportType: 'volume',
    orientation: 'sagittal',
    background: [1, 1, 1],
    toolGroupId: 'mipToolGroup',
    syncGroups: [{
      type: 'voi',
      id: 'ptWLSync',
      source: true,
      target: true
    }, {
      type: 'voi',
      id: 'ptFusionWLSync',
      source: true,
      target: false,
      options: {
        syncInvertState: false
      }
    }],
    // Custom props can be used to set custom properties which extensions
    // can react on.
    customViewportProps: {
      // We use viewportDisplay to filter the viewports which are displayed
      // in mip and we set the scrollbar according to their rotation index
      // in the cornerstone extension.
      hideOverlays: true
    }
  },
  displaySets: [{
    options: {
      blendMode: 'MIP',
      slabThickness: 'fullVolume',
      voi: {
        custom: 'getPTVOIRange'
      },
      voiInverted: true
    },
    id: 'ptDisplaySet'
  }]
};

;// CONCATENATED MODULE: ../../../extensions/tmtv/src/getHangingProtocolModule.js


/**
 * represents a 3x4 viewport layout configuration. The layout displays CT axial, sagittal, and coronal
 * images in the first row, PT axial, sagittal, and coronal images in the second row, and fusion axial,
 * sagittal, and coronal images in the third row. The fourth column is fully spanned by a MIP sagittal
 * image, covering all three rows. It has synchronizers for windowLevel for all CT and PT images, and
 * also camera synchronizer for each orientation
 */
const stage1 = {
  name: 'default',
  viewportStructure: {
    layoutType: 'grid',
    properties: {
      rows: 3,
      columns: 4,
      layoutOptions: [{
        x: 0,
        y: 0,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 1 / 4,
        y: 0,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 2 / 4,
        y: 0,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 0,
        y: 1 / 3,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 1 / 4,
        y: 1 / 3,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 2 / 4,
        y: 1 / 3,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 0,
        y: 2 / 3,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 1 / 4,
        y: 2 / 3,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 2 / 4,
        y: 2 / 3,
        width: 1 / 4,
        height: 1 / 3
      }, {
        x: 3 / 4,
        y: 0,
        width: 1 / 4,
        height: 1
      }]
    }
  },
  viewports: [ctAXIAL, ctSAGITTAL, ctCORONAL, ptAXIAL, ptSAGITTAL, ptCORONAL, fusionAXIAL, fusionSAGITTAL, fusionCORONAL, mipSAGITTAL],
  createdDate: '2021-02-23T18:32:42.850Z'
};

/**
 * The layout displays CT axial image in the top-left viewport, fusion axial image
 * in the top-right viewport, PT axial image in the bottom-left viewport, and MIP
 * sagittal image in the bottom-right viewport. The layout follows a simple grid
 * pattern with 2 rows and 2 columns. It includes synchronizers as well.
 */
const stage2 = {
  name: 'Fusion 2x2',
  viewportStructure: {
    layoutType: 'grid',
    properties: {
      rows: 2,
      columns: 2
    }
  },
  viewports: [ctAXIAL, fusionAXIAL, ptAXIAL, mipSAGITTAL]
};

/**
 * The top row displays CT images in axial, sagittal, and coronal orientations from
 * left to right, respectively. The bottom row displays PT images in axial, sagittal,
 * and coronal orientations from left to right, respectively.
 * The layout follows a simple grid pattern with 2 rows and 3 columns.
 * It includes synchronizers as well.
 */
const stage3 = {
  name: '2x3-layout',
  viewportStructure: {
    layoutType: 'grid',
    properties: {
      rows: 2,
      columns: 3
    }
  },
  viewports: [ctAXIAL, ctSAGITTAL, ctCORONAL, ptAXIAL, ptSAGITTAL, ptCORONAL]
};

/**
 * In this layout, the top row displays PT images in coronal, sagittal, and axial
 * orientations from left to right, respectively, followed by a MIP sagittal image
 * that spans both rows on the rightmost side. The bottom row displays fusion images
 * in coronal, sagittal, and axial orientations from left to right, respectively.
 * There is no viewport in the bottom row's rightmost position, as the MIP sagittal viewport
 * from the top row spans the full height of both rows.
 * It includes synchronizers as well.
 */
const stage4 = {
  name: '2x4-layout',
  viewportStructure: {
    layoutType: 'grid',
    properties: {
      rows: 2,
      columns: 4,
      layoutOptions: [{
        x: 0,
        y: 0,
        width: 1 / 4,
        height: 1 / 2
      }, {
        x: 1 / 4,
        y: 0,
        width: 1 / 4,
        height: 1 / 2
      }, {
        x: 2 / 4,
        y: 0,
        width: 1 / 4,
        height: 1 / 2
      }, {
        x: 3 / 4,
        y: 0,
        width: 1 / 4,
        height: 1
      }, {
        x: 0,
        y: 1 / 2,
        width: 1 / 4,
        height: 1 / 2
      }, {
        x: 1 / 4,
        y: 1 / 2,
        width: 1 / 4,
        height: 1 / 2
      }, {
        x: 2 / 4,
        y: 1 / 2,
        width: 1 / 4,
        height: 1 / 2
      }]
    }
  },
  viewports: [ptCORONAL, ptSAGITTAL, ptAXIAL, mipSAGITTAL, fusionCORONAL, fusionSAGITTAL, fusionAXIAL]
};
const ptCT = {
  id: '@ohif/extension-tmtv.hangingProtocolModule.ptCT',
  locked: true,
  name: 'Default',
  createdDate: '2021-02-23T19:22:08.894Z',
  modifiedDate: '2022-10-04T19:22:08.894Z',
  availableTo: {},
  editableBy: {},
  imageLoadStrategy: 'interleaveTopToBottom',
  // "default" , "interleaveTopToBottom",  "interleaveCenter"
  protocolMatchingRules: [{
    attribute: 'ModalitiesInStudy',
    constraint: {
      contains: ['CT', 'PT']
    }
  }, {
    attribute: 'StudyDescription',
    constraint: {
      contains: 'PETCT'
    }
  }, {
    attribute: 'StudyDescription',
    constraint: {
      contains: 'PET/CT'
    }
  }],
  displaySetSelectors: {
    ctDisplaySet: {
      seriesMatchingRules: [{
        attribute: 'Modality',
        constraint: {
          equals: {
            value: 'CT'
          }
        },
        required: true
      }, {
        attribute: 'isReconstructable',
        constraint: {
          equals: {
            value: true
          }
        },
        required: true
      }, {
        attribute: 'SeriesDescription',
        constraint: {
          contains: 'CT'
        }
      }, {
        attribute: 'SeriesDescription',
        constraint: {
          contains: 'CT WB'
        }
      }]
    },
    ptDisplaySet: {
      seriesMatchingRules: [{
        attribute: 'Modality',
        constraint: {
          equals: 'PT'
        },
        required: true
      }, {
        attribute: 'isReconstructable',
        constraint: {
          equals: {
            value: true
          }
        },
        required: true
      }, {
        attribute: 'SeriesDescription',
        constraint: {
          contains: 'Corrected'
        }
      }, {
        weight: 2,
        attribute: 'SeriesDescription',
        constraint: {
          doesNotContain: {
            value: 'Uncorrected'
          }
        }
      }]
    }
  },
  stages: [stage1, stage2, stage3, stage4],
  numberOfPriorsReferenced: -1
};
function getHangingProtocolModule() {
  return [{
    name: ptCT.id,
    protocol: ptCT
  }];
}
/* harmony default export */ const src_getHangingProtocolModule = (getHangingProtocolModule);
// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var src = __webpack_require__(71783);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var core_src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../../node_modules/react-i18next/dist/es/index.js + 15 modules
var es = __webpack_require__(69190);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/Panels/PanelPetSUV.tsx





const DEFAULT_MEATADATA = {
  PatientWeight: null,
  PatientSex: null,
  SeriesTime: null,
  RadiopharmaceuticalInformationSequence: {
    RadionuclideTotalDose: null,
    RadionuclideHalfLife: null,
    RadiopharmaceuticalStartTime: null
  }
};

/*
 * PETSUV panel enables the user to modify the patient related information, such as
 * patient sex, patientWeight. This is allowed since
 * sometimes these metadata are missing or wrong. By changing them
 * @param param0
 * @returns
 */
function PanelPetSUV(_ref) {
  let {
    servicesManager,
    commandsManager
  } = _ref;
  const {
    t
  } = (0,es/* useTranslation */.$G)('PanelSUV');
  const {
    displaySetService,
    toolGroupService,
    toolbarService,
    hangingProtocolService
  } = servicesManager.services;
  const [metadata, setMetadata] = (0,react.useState)(DEFAULT_MEATADATA);
  const [ptDisplaySet, setPtDisplaySet] = (0,react.useState)(null);
  const handleMetadataChange = metadata => {
    setMetadata(prevState => {
      const newState = {
        ...prevState
      };
      Object.keys(metadata).forEach(key => {
        if (typeof metadata[key] === 'object') {
          newState[key] = {
            ...prevState[key],
            ...metadata[key]
          };
        } else {
          newState[key] = metadata[key];
        }
      });
      return newState;
    });
  };
  const getMatchingPTDisplaySet = viewportMatchDetails => {
    const ptDisplaySet = commandsManager.runCommand('getMatchingPTDisplaySet', {
      viewportMatchDetails
    });
    if (!ptDisplaySet) {
      return;
    }
    const metadata = commandsManager.runCommand('getPTMetadata', {
      ptDisplaySet
    });
    return {
      ptDisplaySet,
      metadata
    };
  };
  (0,react.useEffect)(() => {
    const displaySets = displaySetService.getActiveDisplaySets();
    const {
      viewportMatchDetails
    } = hangingProtocolService.getMatchDetails();
    if (!displaySets.length) {
      return;
    }
    const displaySetInfo = getMatchingPTDisplaySet(viewportMatchDetails);
    if (!displaySetInfo) {
      return;
    }
    const {
      ptDisplaySet,
      metadata
    } = displaySetInfo;
    setPtDisplaySet(ptDisplaySet);
    setMetadata(metadata);
  }, []);

  // get the patientMetadata from the StudyInstanceUIDs and update the state
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = hangingProtocolService.subscribe(hangingProtocolService.EVENTS.PROTOCOL_CHANGED, _ref2 => {
      let {
        viewportMatchDetails
      } = _ref2;
      const displaySetInfo = getMatchingPTDisplaySet(viewportMatchDetails);
      if (!displaySetInfo) {
        return;
      }
      const {
        ptDisplaySet,
        metadata
      } = displaySetInfo;
      setPtDisplaySet(ptDisplaySet);
      setMetadata(metadata);
    });
    return () => {
      unsubscribe();
    };
  }, []);
  function updateMetadata() {
    if (!ptDisplaySet) {
      throw new Error('No ptDisplaySet found');
    }
    const toolGroupIds = toolGroupService.getToolGroupIds();

    // Todo: we don't have a proper way to perform a toggle command and update the
    // state for the toolbar, so here, we manually toggle the toolbar

    // Todo: Crosshairs have bugs for the camera reset currently, so we need to
    // force turn it off before we update the metadata
    toolGroupIds.forEach(toolGroupId => {
      commandsManager.runCommand('toggleCrosshairs', {
        toolGroupId,
        toggledState: false
      });
    });
    toolbarService.state.toggles['Crosshairs'] = false;
    toolbarService._broadcastEvent(toolbarService.EVENTS.TOOL_BAR_STATE_MODIFIED);

    // metadata should be dcmjs naturalized
    core_src.DicomMetadataStore.updateMetadataForSeries(ptDisplaySet.StudyInstanceUID, ptDisplaySet.SeriesInstanceUID, metadata);

    // update the displaySets
    displaySetService.setDisplaySetMetadataInvalidated(ptDisplaySet.displaySetInstanceUID);
  }
  return /*#__PURE__*/react.createElement("div", {
    className: "invisible-scrollbar overflow-y-auto overflow-x-hidden"
  }, /*#__PURE__*/react.createElement("div", {
    className: "flex flex-col"
  }, /*#__PURE__*/react.createElement("div", {
    className: "bg-primary-dark flex flex-col space-y-4 p-4"
  }, /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t('Patient Sex'),
    labelClassName: "text-white mb-2",
    className: "mt-1",
    value: metadata.PatientSex || '',
    onChange: e => {
      handleMetadataChange({
        PatientSex: e.target.value
      });
    }
  }), /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t('Patient Weight (kg)'),
    labelClassName: "text-white mb-2",
    className: "mt-1",
    value: metadata.PatientWeight || '',
    onChange: e => {
      handleMetadataChange({
        PatientWeight: e.target.value
      });
    }
  }), /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t('Total Dose (bq)'),
    labelClassName: "text-white mb-2",
    className: "mt-1",
    value: metadata.RadiopharmaceuticalInformationSequence.RadionuclideTotalDose || '',
    onChange: e => {
      handleMetadataChange({
        RadiopharmaceuticalInformationSequence: {
          RadionuclideTotalDose: e.target.value
        }
      });
    }
  }), /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t('Half Life (s)'),
    labelClassName: "text-white mb-2",
    className: "mt-1",
    value: metadata.RadiopharmaceuticalInformationSequence.RadionuclideHalfLife || '',
    onChange: e => {
      handleMetadataChange({
        RadiopharmaceuticalInformationSequence: {
          RadionuclideHalfLife: e.target.value
        }
      });
    }
  }), /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t('Injection Time (s)'),
    labelClassName: "text-white mb-2",
    className: "mt-1",
    value: metadata.RadiopharmaceuticalInformationSequence.RadiopharmaceuticalStartTime || '',
    onChange: e => {
      handleMetadataChange({
        RadiopharmaceuticalInformationSequence: {
          RadiopharmaceuticalStartTime: e.target.value
        }
      });
    }
  }), /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t('Acquisition Time (s)'),
    labelClassName: "text-white mb-2",
    className: "mt-1 mb-2",
    value: metadata.SeriesTime || '',
    onChange: () => {}
  }), /*#__PURE__*/react.createElement(src/* Button */.zx, {
    onClick: updateMetadata
  }, "Reload Data"))));
}
PanelPetSUV.propTypes = {
  servicesManager: prop_types_default().shape({
    services: prop_types_default().shape({
      measurementService: prop_types_default().shape({
        getMeasurements: (prop_types_default()).func.isRequired,
        subscribe: (prop_types_default()).func.isRequired,
        EVENTS: (prop_types_default()).object.isRequired,
        VALUE_TYPES: (prop_types_default()).object.isRequired
      }).isRequired
    }).isRequired
  }).isRequired
};
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/Panels/PanelROIThresholdSegmentation/segmentationEditHandler.tsx


function segmentationItemEditHandler(_ref) {
  let {
    id,
    servicesManager
  } = _ref;
  const {
    segmentationService,
    uiDialogService
  } = servicesManager.services;
  const segmentation = segmentationService.getSegmentation(id);
  const onSubmitHandler = _ref2 => {
    let {
      action,
      value
    } = _ref2;
    switch (action.id) {
      case 'save':
        {
          segmentationService.addOrUpdateSegmentation({
            ...segmentation,
            ...value
          }, false,
          // don't suppress event
          true // it should update cornerstone
          );
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
    content: src/* Dialog */.Vq,
    contentProps: {
      title: 'Enter your Segmentation',
      noCloseButton: true,
      value: {
        label: segmentation.label || ''
      },
      body: _ref3 => {
        let {
          value,
          setValue
        } = _ref3;
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
        return /*#__PURE__*/react.createElement(src/* Input */.II, {
          autoFocus: true,
          className: "border-primary-main bg-black",
          type: "text",
          containerClassName: "mr-2",
          value: value.label,
          onChange: onChangeHandler,
          onKeyPress: onKeyPressHandler
        });
      },
      actions: [{
        id: 'cancel',
        text: 'Cancel',
        type: src/* ButtonEnums.type */.LZ.dt.secondary
      }, {
        id: 'save',
        text: 'Save',
        type: src/* ButtonEnums.type */.LZ.dt.primary
      }],
      onSubmit: onSubmitHandler
    }
  });
}
/* harmony default export */ const segmentationEditHandler = (segmentationItemEditHandler);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/Panels/PanelROIThresholdSegmentation/ExportReports.tsx



function ExportReports(_ref) {
  let {
    segmentations,
    tmtvValue,
    config,
    commandsManager
  } = _ref;
  const {
    t
  } = (0,es/* useTranslation */.$G)('PanelSUVExport');
  return /*#__PURE__*/react.createElement(react.Fragment, null, segmentations?.length ? /*#__PURE__*/react.createElement("div", {
    className: "mt-4 flex justify-center space-x-2"
  }, /*#__PURE__*/react.createElement(src/* LegacyButtonGroup */.HO, {
    color: "black",
    size: "inherit"
  }, /*#__PURE__*/react.createElement(src/* LegacyButton */.mN, {
    className: "px-2 py-2 text-base",
    disabled: tmtvValue === null,
    onClick: () => {
      commandsManager.runCommand('exportTMTVReportCSV', {
        segmentations,
        tmtv: tmtvValue,
        config
      });
    }
  }, t('Export CSV'))), /*#__PURE__*/react.createElement(src/* LegacyButtonGroup */.HO, {
    color: "black",
    size: "inherit"
  }, /*#__PURE__*/react.createElement(src/* LegacyButton */.mN, {
    className: "px-2 py-2 text-base",
    onClick: () => {
      commandsManager.runCommand('createTMTVRTReport');
    },
    disabled: tmtvValue === null
  }, t('Create RT Report')))) : null);
}
/* harmony default export */ const PanelROIThresholdSegmentation_ExportReports = (ExportReports);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/Panels/PanelROIThresholdSegmentation/ROIThresholdConfiguration.tsx



const ROI_STAT = 'roi_stat';
const RANGE = 'range';
const options = [{
  value: ROI_STAT,
  label: 'Max',
  placeHolder: 'Max'
}, {
  value: RANGE,
  label: 'Range',
  placeHolder: 'Range'
}];
function ROIThresholdConfiguration(_ref) {
  let {
    config,
    dispatch,
    runCommand
  } = _ref;
  const {
    t
  } = (0,es/* useTranslation */.$G)('ROIThresholdConfiguration');
  return /*#__PURE__*/react.createElement("div", {
    className: "bg-primary-dark flex flex-col space-y-4 px-4 py-2"
  }, /*#__PURE__*/react.createElement("div", {
    className: "flex items-end space-x-2"
  }, /*#__PURE__*/react.createElement("div", {
    className: "flex w-1/2 flex-col"
  }, /*#__PURE__*/react.createElement(src/* Select */.Ph, {
    label: t('Strategy'),
    closeMenuOnSelect: true,
    className: "border-primary-main mr-2 bg-black text-white ",
    options: options,
    placeholder: options.find(option => option.value === config.strategy).placeHolder,
    value: config.strategy,
    onChange: _ref2 => {
      let {
        value
      } = _ref2;
      dispatch({
        type: 'setStrategy',
        payload: {
          strategy: value
        }
      });
    }
  })), /*#__PURE__*/react.createElement("div", {
    className: "w-1/2"
  }, /*#__PURE__*/react.createElement(src/* LegacyButtonGroup */.HO, null, /*#__PURE__*/react.createElement(src/* LegacyButton */.mN, {
    size: "initial",
    className: "px-2 py-2 text-base text-white",
    color: "primaryLight",
    variant: "outlined",
    onClick: () => runCommand('setStartSliceForROIThresholdTool')
  }, t('Start')), /*#__PURE__*/react.createElement(src/* LegacyButton */.mN, {
    size: "initial",
    color: "primaryLight",
    variant: "outlined",
    className: "px-2 py-2 text-base text-white",
    onClick: () => runCommand('setEndSliceForROIThresholdTool')
  }, t('End'))))), config.strategy === ROI_STAT && /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t('Percentage of Max SUV'),
    labelClassName: "text-white",
    className: "border-primary-main mt-2 bg-black",
    type: "text",
    containerClassName: "mr-2",
    value: config.weight,
    onChange: e => {
      dispatch({
        type: 'setWeight',
        payload: {
          weight: e.target.value
        }
      });
    }
  }), config.strategy !== ROI_STAT && /*#__PURE__*/react.createElement("div", {
    className: "mr-2 text-sm"
  }, /*#__PURE__*/react.createElement("table", null, /*#__PURE__*/react.createElement("tbody", null, /*#__PURE__*/react.createElement("tr", {
    className: "mt-2"
  }, /*#__PURE__*/react.createElement("td", {
    className: "pr-4 pt-2",
    colSpan: "3"
  }, /*#__PURE__*/react.createElement(src/* Label */.__, {
    className: "text-white",
    text: "Lower & Upper Ranges"
  }))), /*#__PURE__*/react.createElement("tr", {
    className: "mt-2"
  }, /*#__PURE__*/react.createElement("td", {
    className: "pr-4 pt-2 text-center"
  }, /*#__PURE__*/react.createElement(src/* Label */.__, {
    className: "text-white",
    text: "CT"
  })), /*#__PURE__*/react.createElement("td", null, /*#__PURE__*/react.createElement("div", {
    className: "flex justify-between"
  }, /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t(''),
    labelClassName: "text-white",
    className: "border-primary-main mt-2 bg-black",
    type: "text",
    containerClassName: "mr-2",
    value: config.ctLower,
    onChange: e => {
      dispatch({
        type: 'setThreshold',
        payload: {
          ctLower: e.target.value
        }
      });
    }
  }), /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t(''),
    labelClassName: "text-white",
    className: "border-primary-main mt-2 bg-black",
    type: "text",
    containerClassName: "mr-2",
    value: config.ctUpper,
    onChange: e => {
      dispatch({
        type: 'setThreshold',
        payload: {
          ctUpper: e.target.value
        }
      });
    }
  })))), /*#__PURE__*/react.createElement("tr", null, /*#__PURE__*/react.createElement("td", {
    className: "pr-4 pt-2 text-center"
  }, /*#__PURE__*/react.createElement(src/* Label */.__, {
    className: "text-white",
    text: "PT"
  })), /*#__PURE__*/react.createElement("td", null, /*#__PURE__*/react.createElement("div", {
    className: "flex justify-between"
  }, /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t(''),
    labelClassName: "text-white",
    className: "border-primary-main mt-2 bg-black",
    type: "text",
    containerClassName: "mr-2",
    value: config.ptLower,
    onChange: e => {
      dispatch({
        type: 'setThreshold',
        payload: {
          ptLower: e.target.value
        }
      });
    }
  }), /*#__PURE__*/react.createElement(src/* Input */.II, {
    label: t(''),
    labelClassName: "text-white",
    className: "border-primary-main mt-2 bg-black",
    type: "text",
    containerClassName: "mr-2",
    value: config.ptUpper,
    onChange: e => {
      dispatch({
        type: 'setThreshold',
        payload: {
          ptUpper: e.target.value
        }
      });
    }
  }))))))));
}
/* harmony default export */ const PanelROIThresholdSegmentation_ROIThresholdConfiguration = (ROIThresholdConfiguration);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/Panels/PanelROIThresholdSegmentation/PanelROIThresholdSegmentation.tsx







const LOWER_CT_THRESHOLD_DEFAULT = -1024;
const UPPER_CT_THRESHOLD_DEFAULT = 1024;
const LOWER_PT_THRESHOLD_DEFAULT = 2.5;
const UPPER_PT_THRESHOLD_DEFAULT = 100;
const WEIGHT_DEFAULT = 0.41; // a default weight for suv max often used in the literature
const DEFAULT_STRATEGY = ROI_STAT;
function reducer(state, action) {
  const {
    payload
  } = action;
  const {
    strategy,
    ctLower,
    ctUpper,
    ptLower,
    ptUpper,
    weight
  } = payload;
  switch (action.type) {
    case 'setStrategy':
      return {
        ...state,
        strategy
      };
    case 'setThreshold':
      return {
        ...state,
        ctLower: ctLower ? ctLower : state.ctLower,
        ctUpper: ctUpper ? ctUpper : state.ctUpper,
        ptLower: ptLower ? ptLower : state.ptLower,
        ptUpper: ptUpper ? ptUpper : state.ptUpper
      };
    case 'setWeight':
      return {
        ...state,
        weight
      };
    default:
      return state;
  }
}
function PanelRoiThresholdSegmentation(_ref) {
  let {
    servicesManager,
    commandsManager
  } = _ref;
  const {
    segmentationService
  } = servicesManager.services;
  const {
    t
  } = (0,es/* useTranslation */.$G)('PanelSUV');
  const [showConfig, setShowConfig] = (0,react.useState)(false);
  const [labelmapLoading, setLabelmapLoading] = (0,react.useState)(false);
  const [selectedSegmentationId, setSelectedSegmentationId] = (0,react.useState)(null);
  const [segmentations, setSegmentations] = (0,react.useState)(() => segmentationService.getSegmentations());
  const [config, dispatch] = (0,react.useReducer)(reducer, {
    strategy: DEFAULT_STRATEGY,
    ctLower: LOWER_CT_THRESHOLD_DEFAULT,
    ctUpper: UPPER_CT_THRESHOLD_DEFAULT,
    ptLower: LOWER_PT_THRESHOLD_DEFAULT,
    ptUpper: UPPER_PT_THRESHOLD_DEFAULT,
    weight: WEIGHT_DEFAULT
  });
  const [tmtvValue, setTmtvValue] = (0,react.useState)(null);
  const runCommand = (0,react.useCallback)(function (commandName) {
    let commandOptions = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
    return commandsManager.runCommand(commandName, commandOptions);
  }, [commandsManager]);
  const handleTMTVCalculation = (0,react.useCallback)(() => {
    const tmtv = runCommand('calculateTMTV', {
      segmentations
    });
    if (tmtv !== undefined) {
      setTmtvValue(tmtv.toFixed(2));
    }
  }, [segmentations, runCommand]);
  const handleROIThresholding = (0,react.useCallback)(() => {
    const labelmap = runCommand('thresholdSegmentationByRectangleROITool', {
      segmentationId: selectedSegmentationId,
      config
    });
    const lesionStats = runCommand('getLesionStats', {
      labelmap
    });
    const suvPeak = runCommand('calculateSuvPeak', {
      labelmap
    });
    const lesionGlyoclysisStats = lesionStats.volume * lesionStats.meanValue;

    // update segDetails with the suv peak for the active segmentation
    const segmentation = segmentationService.getSegmentation(selectedSegmentationId);
    const cachedStats = {
      lesionStats,
      suvPeak,
      lesionGlyoclysisStats
    };
    const notYetUpdatedAtSource = true;
    segmentationService.addOrUpdateSegmentation({
      ...segmentation,
      ...Object.assign(segmentation.cachedStats, cachedStats),
      displayText: [`SUV Peak: ${suvPeak.suvPeak.toFixed(2)}`]
    }, notYetUpdatedAtSource);
    handleTMTVCalculation();
  }, [selectedSegmentationId, config]);

  /**
   * Update UI based on segmentation changes (added, removed, updated)
   */
  (0,react.useEffect)(() => {
    // ~~ Subscription
    const added = segmentationService.EVENTS.SEGMENTATION_ADDED;
    const updated = segmentationService.EVENTS.SEGMENTATION_UPDATED;
    const subscriptions = [];
    [added, updated].forEach(evt => {
      const {
        unsubscribe
      } = segmentationService.subscribe(evt, () => {
        const segmentations = segmentationService.getSegmentations();
        setSegmentations(segmentations);
      });
      subscriptions.push(unsubscribe);
    });
    return () => {
      subscriptions.forEach(unsub => {
        unsub();
      });
    };
  }, []);
  (0,react.useEffect)(() => {
    const {
      unsubscribe
    } = segmentationService.subscribe(segmentationService.EVENTS.SEGMENTATION_REMOVED, () => {
      const segmentations = segmentationService.getSegmentations();
      setSegmentations(segmentations);
      if (segmentations.length > 0) {
        setSelectedSegmentationId(segmentations[0].id);
        handleTMTVCalculation();
      } else {
        setSelectedSegmentationId(null);
        setTmtvValue(null);
      }
    });
    return () => {
      unsubscribe();
    };
  }, []);

  /**
   * Whenever the segmentations change, update the TMTV calculations
   */
  (0,react.useEffect)(() => {
    if (!selectedSegmentationId && segmentations.length > 0) {
      setSelectedSegmentationId(segmentations[0].id);
    }
    handleTMTVCalculation();
  }, [segmentations, selectedSegmentationId]);
  return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("div", {
    className: "flex flex-col"
  }, /*#__PURE__*/react.createElement("div", {
    className: "invisible-scrollbar overflow-y-auto overflow-x-hidden"
  }, /*#__PURE__*/react.createElement("div", {
    className: "mx-4 my-4 mb-4 flex space-x-4"
  }, /*#__PURE__*/react.createElement(src/* Button */.zx, {
    onClick: () => {
      setLabelmapLoading(true);
      setTimeout(() => {
        runCommand('createNewLabelmapFromPT').then(segmentationId => {
          setLabelmapLoading(false);
          setSelectedSegmentationId(segmentationId);
        });
      });
    }
  }, labelmapLoading ? 'loading ...' : 'New Label'), /*#__PURE__*/react.createElement(src/* Button */.zx, {
    onClick: handleROIThresholding
  }, "Run")), /*#__PURE__*/react.createElement("div", {
    className: "bg-secondary-dark border-secondary-light mb-2 flex h-8 cursor-pointer select-none items-center justify-around border-t outline-none first:border-0",
    onClick: () => {
      setShowConfig(!showConfig);
    }
  }, /*#__PURE__*/react.createElement("div", {
    className: "px-4 text-base text-white"
  }, t('ROI Threshold Configuration'))), showConfig && /*#__PURE__*/react.createElement(PanelROIThresholdSegmentation_ROIThresholdConfiguration, {
    config: config,
    dispatch: dispatch,
    runCommand: runCommand
  }), /*#__PURE__*/react.createElement("div", {
    className: "mt-4"
  }, segmentations?.length ? /*#__PURE__*/react.createElement(src/* SegmentationTable */.y1, {
    title: t('Segmentations'),
    segmentations: segmentations,
    activeSegmentationId: selectedSegmentationId,
    onClick: id => {
      runCommand('setSegmentationActiveForToolGroups', {
        segmentationId: id
      });
      setSelectedSegmentationId(id);
    },
    onToggleVisibility: id => {
      segmentationService.toggleSegmentationVisibility(id);
    },
    onToggleVisibilityAll: ids => {
      ids.map(id => {
        segmentationService.toggleSegmentationVisibility(id);
      });
    },
    onDelete: id => {
      segmentationService.remove(id);
    },
    onEdit: id => {
      segmentationEditHandler({
        id,
        servicesManager
      });
    }
  }) : null), tmtvValue !== null ? /*#__PURE__*/react.createElement("div", {
    className: "bg-secondary-dark mt-4 flex items-baseline justify-between px-2 py-1"
  }, /*#__PURE__*/react.createElement("span", {
    className: "text-base font-bold uppercase tracking-widest text-white"
  }, 'TMTV:'), /*#__PURE__*/react.createElement("div", {
    className: "text-white"
  }, `${tmtvValue} mL`)) : null, /*#__PURE__*/react.createElement(PanelROIThresholdSegmentation_ExportReports, {
    segmentations: segmentations,
    tmtvValue: tmtvValue,
    config: config,
    commandsManager: commandsManager
  }))), /*#__PURE__*/react.createElement("div", {
    className: "mt-auto mb-4 flex cursor-pointer items-center justify-center text-blue-400 opacity-50 hover:opacity-80",
    onClick: () => {
      // navigate to a url in a new tab
      window.open('https://github.com/OHIF/Viewers/blob/master/modes/tmtv/README.md', '_blank');
    }
  }, /*#__PURE__*/react.createElement(src/* Icon */.JO, {
    width: "15px",
    height: "15px",
    name: 'info',
    className: 'text-primary-active ml-4 mr-3'
  }), /*#__PURE__*/react.createElement("span", null, 'User Guide')));
}
PanelRoiThresholdSegmentation.propTypes = {
  commandsManager: prop_types_default().shape({
    runCommand: (prop_types_default()).func.isRequired
  }),
  servicesManager: prop_types_default().shape({
    services: prop_types_default().shape({
      segmentationService: prop_types_default().shape({
        getSegmentation: (prop_types_default()).func.isRequired,
        getSegmentations: (prop_types_default()).func.isRequired,
        toggleSegmentationVisibility: (prop_types_default()).func.isRequired,
        subscribe: (prop_types_default()).func.isRequired,
        EVENTS: (prop_types_default()).object.isRequired
      }).isRequired
    }).isRequired
  }).isRequired
};
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/Panels/PanelROIThresholdSegmentation/index.ts

/* harmony default export */ const PanelROIThresholdSegmentation = (PanelRoiThresholdSegmentation);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/Panels/index.tsx



;// CONCATENATED MODULE: ../../../extensions/tmtv/src/getPanelModule.tsx



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
  const wrappedPanelPetSuv = () => {
    return /*#__PURE__*/react.createElement(PanelPetSUV, {
      commandsManager: commandsManager,
      servicesManager: servicesManager,
      extensionManager: extensionManager
    });
  };
  const wrappedROIThresholdSeg = () => {
    return /*#__PURE__*/react.createElement(PanelROIThresholdSegmentation, {
      commandsManager: commandsManager,
      servicesManager: servicesManager,
      extensionManager: extensionManager
    });
  };
  return [{
    name: 'petSUV',
    iconName: 'tab-patient-info',
    iconLabel: 'PET SUV',
    label: 'PET SUV',
    component: wrappedPanelPetSuv
  }, {
    name: 'ROIThresholdSeg',
    iconName: 'tab-roi-threshold',
    iconLabel: 'ROI Threshold',
    label: 'ROI Threshold',
    component: wrappedROIThresholdSeg
  }];
}
/* harmony default export */ const src_getPanelModule = (getPanelModule);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/tools/dist/esm/index.js + 348 modules
var esm = __webpack_require__(14957);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var dist_esm = __webpack_require__(3743);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/measurementServiceMappings/constants/supportedTools.js
/* harmony default export */ const supportedTools = (['RectangleROIStartEndThreshold']);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/measurementServiceMappings/utils/getSOPInstanceAttributes.js

function getSOPInstanceAttributes(imageId) {
  if (imageId) {
    return _getUIDFromImageID(imageId);
  }
}
function _getUIDFromImageID(imageId) {
  const instance = dist_esm.metaData.get('instance', imageId);
  return {
    SOPInstanceUID: instance.SOPInstanceUID,
    SeriesInstanceUID: instance.SeriesInstanceUID,
    StudyInstanceUID: instance.StudyInstanceUID,
    frameNumber: instance.frameNumber || 1
  };
}
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/measurementServiceMappings/RectangleROIStartEndThreshold.js


const RectangleROIStartEndThreshold = {
  toAnnotation: (measurement, definition) => {},
  /**
   * Maps cornerstone annotation event data to measurement service format.
   *
   * @param {Object} cornerstone Cornerstone event data
   * @return {Measurement} Measurement instance
   */
  toMeasurement: (csToolsEventDetail, displaySetService, cornerstoneViewportService) => {
    const {
      annotation,
      viewportId
    } = csToolsEventDetail;
    const {
      metadata,
      data,
      annotationUID
    } = annotation;
    if (!metadata || !data) {
      console.warn('Length tool: Missing metadata or data');
      return null;
    }
    const {
      toolName,
      referencedImageId,
      FrameOfReferenceUID
    } = metadata;
    const validToolType = supportedTools.includes(toolName);
    if (!validToolType) {
      throw new Error('Tool not supported');
    }
    const {
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID
    } = getSOPInstanceAttributes(referencedImageId, cornerstoneViewportService, viewportId);
    let displaySet;
    if (SOPInstanceUID) {
      displaySet = displaySetService.getDisplaySetForSOPInstanceUID(SOPInstanceUID, SeriesInstanceUID);
    } else {
      displaySet = displaySetService.getDisplaySetsForSeries(SeriesInstanceUID);
    }
    const {
      cachedStats
    } = data;
    return {
      uid: annotationUID,
      SOPInstanceUID,
      FrameOfReferenceUID,
      // points,
      metadata,
      referenceSeriesUID: SeriesInstanceUID,
      referenceStudyUID: StudyInstanceUID,
      toolName: metadata.toolName,
      displaySetInstanceUID: displaySet.displaySetInstanceUID,
      label: metadata.label,
      // displayText: displayText,
      data: data.cachedStats,
      type: 'RectangleROIStartEndThreshold'
      // getReport,
    };
  }
};

/* harmony default export */ const measurementServiceMappings_RectangleROIStartEndThreshold = (RectangleROIStartEndThreshold);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/measurementServiceMappings/measurementServiceMappingsFactory.js

const measurementServiceMappingsFactory = (measurementService, displaySetService, cornerstoneViewportService) => {
  return {
    RectangleROIStartEndThreshold: {
      toAnnotation: measurementServiceMappings_RectangleROIStartEndThreshold.toAnnotation,
      toMeasurement: csToolsAnnotation => measurementServiceMappings_RectangleROIStartEndThreshold.toMeasurement(csToolsAnnotation, displaySetService, cornerstoneViewportService),
      matchingCriteria: [{
        valueType: measurementService.VALUE_TYPES.ROI_THRESHOLD_MANUAL
      }]
    }
  };
};
/* harmony default export */ const measurementServiceMappings_measurementServiceMappingsFactory = (measurementServiceMappingsFactory);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/colormaps/index.js
/* harmony default export */ const colormaps = ([{
  ColorSpace: 'RGB',
  Name: 'hot_iron',
  RGBPoints: [0.0, 0.0039215686, 0.0039215686, 0.0156862745, 0.00392156862745098, 0.0039215686, 0.0039215686, 0.0156862745, 0.00784313725490196, 0.0039215686, 0.0039215686, 0.031372549, 0.011764705882352941, 0.0039215686, 0.0039215686, 0.0470588235, 0.01568627450980392, 0.0039215686, 0.0039215686, 0.062745098, 0.0196078431372549, 0.0039215686, 0.0039215686, 0.0784313725, 0.023529411764705882, 0.0039215686, 0.0039215686, 0.0941176471, 0.027450980392156862, 0.0039215686, 0.0039215686, 0.1098039216, 0.03137254901960784, 0.0039215686, 0.0039215686, 0.1254901961, 0.03529411764705882, 0.0039215686, 0.0039215686, 0.1411764706, 0.0392156862745098, 0.0039215686, 0.0039215686, 0.1568627451, 0.043137254901960784, 0.0039215686, 0.0039215686, 0.1725490196, 0.047058823529411764, 0.0039215686, 0.0039215686, 0.1882352941, 0.050980392156862744, 0.0039215686, 0.0039215686, 0.2039215686, 0.054901960784313725, 0.0039215686, 0.0039215686, 0.2196078431, 0.05882352941176471, 0.0039215686, 0.0039215686, 0.2352941176, 0.06274509803921569, 0.0039215686, 0.0039215686, 0.2509803922, 0.06666666666666667, 0.0039215686, 0.0039215686, 0.262745098, 0.07058823529411765, 0.0039215686, 0.0039215686, 0.2784313725, 0.07450980392156863, 0.0039215686, 0.0039215686, 0.2941176471, 0.0784313725490196, 0.0039215686, 0.0039215686, 0.3098039216, 0.08235294117647059, 0.0039215686, 0.0039215686, 0.3254901961, 0.08627450980392157, 0.0039215686, 0.0039215686, 0.3411764706, 0.09019607843137255, 0.0039215686, 0.0039215686, 0.3568627451, 0.09411764705882353, 0.0039215686, 0.0039215686, 0.3725490196, 0.09803921568627451, 0.0039215686, 0.0039215686, 0.3882352941, 0.10196078431372549, 0.0039215686, 0.0039215686, 0.4039215686, 0.10588235294117647, 0.0039215686, 0.0039215686, 0.4196078431, 0.10980392156862745, 0.0039215686, 0.0039215686, 0.4352941176, 0.11372549019607843, 0.0039215686, 0.0039215686, 0.4509803922, 0.11764705882352942, 0.0039215686, 0.0039215686, 0.4666666667, 0.12156862745098039, 0.0039215686, 0.0039215686, 0.4823529412, 0.12549019607843137, 0.0039215686, 0.0039215686, 0.4980392157, 0.12941176470588237, 0.0039215686, 0.0039215686, 0.5137254902, 0.13333333333333333, 0.0039215686, 0.0039215686, 0.5294117647, 0.13725490196078433, 0.0039215686, 0.0039215686, 0.5450980392, 0.1411764705882353, 0.0039215686, 0.0039215686, 0.5607843137, 0.1450980392156863, 0.0039215686, 0.0039215686, 0.5764705882, 0.14901960784313725, 0.0039215686, 0.0039215686, 0.5921568627, 0.15294117647058825, 0.0039215686, 0.0039215686, 0.6078431373, 0.1568627450980392, 0.0039215686, 0.0039215686, 0.6235294118, 0.1607843137254902, 0.0039215686, 0.0039215686, 0.6392156863, 0.16470588235294117, 0.0039215686, 0.0039215686, 0.6549019608, 0.16862745098039217, 0.0039215686, 0.0039215686, 0.6705882353, 0.17254901960784313, 0.0039215686, 0.0039215686, 0.6862745098, 0.17647058823529413, 0.0039215686, 0.0039215686, 0.7019607843, 0.1803921568627451, 0.0039215686, 0.0039215686, 0.7176470588, 0.1843137254901961, 0.0039215686, 0.0039215686, 0.7333333333, 0.18823529411764706, 0.0039215686, 0.0039215686, 0.7490196078, 0.19215686274509805, 0.0039215686, 0.0039215686, 0.7607843137, 0.19607843137254902, 0.0039215686, 0.0039215686, 0.7764705882, 0.2, 0.0039215686, 0.0039215686, 0.7921568627, 0.20392156862745098, 0.0039215686, 0.0039215686, 0.8078431373, 0.20784313725490197, 0.0039215686, 0.0039215686, 0.8235294118, 0.21176470588235294, 0.0039215686, 0.0039215686, 0.8392156863, 0.21568627450980393, 0.0039215686, 0.0039215686, 0.8549019608, 0.2196078431372549, 0.0039215686, 0.0039215686, 0.8705882353, 0.2235294117647059, 0.0039215686, 0.0039215686, 0.8862745098, 0.22745098039215686, 0.0039215686, 0.0039215686, 0.9019607843, 0.23137254901960785, 0.0039215686, 0.0039215686, 0.9176470588, 0.23529411764705885, 0.0039215686, 0.0039215686, 0.9333333333, 0.23921568627450984, 0.0039215686, 0.0039215686, 0.9490196078, 0.24313725490196078, 0.0039215686, 0.0039215686, 0.9647058824, 0.24705882352941178, 0.0039215686, 0.0039215686, 0.9803921569, 0.25098039215686274, 0.0039215686, 0.0039215686, 0.9960784314, 0.2549019607843137, 0.0039215686, 0.0039215686, 0.9960784314, 0.25882352941176473, 0.0156862745, 0.0039215686, 0.9803921569, 0.2627450980392157, 0.031372549, 0.0039215686, 0.9647058824, 0.26666666666666666, 0.0470588235, 0.0039215686, 0.9490196078, 0.27058823529411763, 0.062745098, 0.0039215686, 0.9333333333, 0.27450980392156865, 0.0784313725, 0.0039215686, 0.9176470588, 0.2784313725490196, 0.0941176471, 0.0039215686, 0.9019607843, 0.2823529411764706, 0.1098039216, 0.0039215686, 0.8862745098, 0.28627450980392155, 0.1254901961, 0.0039215686, 0.8705882353, 0.2901960784313726, 0.1411764706, 0.0039215686, 0.8549019608, 0.29411764705882354, 0.1568627451, 0.0039215686, 0.8392156863, 0.2980392156862745, 0.1725490196, 0.0039215686, 0.8235294118, 0.30196078431372547, 0.1882352941, 0.0039215686, 0.8078431373, 0.3058823529411765, 0.2039215686, 0.0039215686, 0.7921568627, 0.30980392156862746, 0.2196078431, 0.0039215686, 0.7764705882, 0.3137254901960784, 0.2352941176, 0.0039215686, 0.7607843137, 0.3176470588235294, 0.2509803922, 0.0039215686, 0.7490196078, 0.3215686274509804, 0.262745098, 0.0039215686, 0.7333333333, 0.3254901960784314, 0.2784313725, 0.0039215686, 0.7176470588, 0.32941176470588235, 0.2941176471, 0.0039215686, 0.7019607843, 0.3333333333333333, 0.3098039216, 0.0039215686, 0.6862745098, 0.33725490196078434, 0.3254901961, 0.0039215686, 0.6705882353, 0.3411764705882353, 0.3411764706, 0.0039215686, 0.6549019608, 0.34509803921568627, 0.3568627451, 0.0039215686, 0.6392156863, 0.34901960784313724, 0.3725490196, 0.0039215686, 0.6235294118, 0.35294117647058826, 0.3882352941, 0.0039215686, 0.6078431373, 0.3568627450980392, 0.4039215686, 0.0039215686, 0.5921568627, 0.3607843137254902, 0.4196078431, 0.0039215686, 0.5764705882, 0.36470588235294116, 0.4352941176, 0.0039215686, 0.5607843137, 0.3686274509803922, 0.4509803922, 0.0039215686, 0.5450980392, 0.37254901960784315, 0.4666666667, 0.0039215686, 0.5294117647, 0.3764705882352941, 0.4823529412, 0.0039215686, 0.5137254902, 0.3803921568627451, 0.4980392157, 0.0039215686, 0.4980392157, 0.3843137254901961, 0.5137254902, 0.0039215686, 0.4823529412, 0.38823529411764707, 0.5294117647, 0.0039215686, 0.4666666667, 0.39215686274509803, 0.5450980392, 0.0039215686, 0.4509803922, 0.396078431372549, 0.5607843137, 0.0039215686, 0.4352941176, 0.4, 0.5764705882, 0.0039215686, 0.4196078431, 0.403921568627451, 0.5921568627, 0.0039215686, 0.4039215686, 0.40784313725490196, 0.6078431373, 0.0039215686, 0.3882352941, 0.4117647058823529, 0.6235294118, 0.0039215686, 0.3725490196, 0.41568627450980394, 0.6392156863, 0.0039215686, 0.3568627451, 0.4196078431372549, 0.6549019608, 0.0039215686, 0.3411764706, 0.4235294117647059, 0.6705882353, 0.0039215686, 0.3254901961, 0.42745098039215684, 0.6862745098, 0.0039215686, 0.3098039216, 0.43137254901960786, 0.7019607843, 0.0039215686, 0.2941176471, 0.43529411764705883, 0.7176470588, 0.0039215686, 0.2784313725, 0.4392156862745098, 0.7333333333, 0.0039215686, 0.262745098, 0.44313725490196076, 0.7490196078, 0.0039215686, 0.2509803922, 0.4470588235294118, 0.7607843137, 0.0039215686, 0.2352941176, 0.45098039215686275, 0.7764705882, 0.0039215686, 0.2196078431, 0.4549019607843137, 0.7921568627, 0.0039215686, 0.2039215686, 0.4588235294117647, 0.8078431373, 0.0039215686, 0.1882352941, 0.4627450980392157, 0.8235294118, 0.0039215686, 0.1725490196, 0.4666666666666667, 0.8392156863, 0.0039215686, 0.1568627451, 0.4705882352941177, 0.8549019608, 0.0039215686, 0.1411764706, 0.4745098039215686, 0.8705882353, 0.0039215686, 0.1254901961, 0.4784313725490197, 0.8862745098, 0.0039215686, 0.1098039216, 0.48235294117647065, 0.9019607843, 0.0039215686, 0.0941176471, 0.48627450980392156, 0.9176470588, 0.0039215686, 0.0784313725, 0.49019607843137253, 0.9333333333, 0.0039215686, 0.062745098, 0.49411764705882355, 0.9490196078, 0.0039215686, 0.0470588235, 0.4980392156862745, 0.9647058824, 0.0039215686, 0.031372549, 0.5019607843137255, 0.9803921569, 0.0039215686, 0.0156862745, 0.5058823529411764, 0.9960784314, 0.0039215686, 0.0039215686, 0.5098039215686274, 0.9960784314, 0.0156862745, 0.0039215686, 0.5137254901960784, 0.9960784314, 0.031372549, 0.0039215686, 0.5176470588235295, 0.9960784314, 0.0470588235, 0.0039215686, 0.5215686274509804, 0.9960784314, 0.062745098, 0.0039215686, 0.5254901960784314, 0.9960784314, 0.0784313725, 0.0039215686, 0.5294117647058824, 0.9960784314, 0.0941176471, 0.0039215686, 0.5333333333333333, 0.9960784314, 0.1098039216, 0.0039215686, 0.5372549019607843, 0.9960784314, 0.1254901961, 0.0039215686, 0.5411764705882353, 0.9960784314, 0.1411764706, 0.0039215686, 0.5450980392156862, 0.9960784314, 0.1568627451, 0.0039215686, 0.5490196078431373, 0.9960784314, 0.1725490196, 0.0039215686, 0.5529411764705883, 0.9960784314, 0.1882352941, 0.0039215686, 0.5568627450980392, 0.9960784314, 0.2039215686, 0.0039215686, 0.5607843137254902, 0.9960784314, 0.2196078431, 0.0039215686, 0.5647058823529412, 0.9960784314, 0.2352941176, 0.0039215686, 0.5686274509803921, 0.9960784314, 0.2509803922, 0.0039215686, 0.5725490196078431, 0.9960784314, 0.262745098, 0.0039215686, 0.5764705882352941, 0.9960784314, 0.2784313725, 0.0039215686, 0.5803921568627451, 0.9960784314, 0.2941176471, 0.0039215686, 0.5843137254901961, 0.9960784314, 0.3098039216, 0.0039215686, 0.5882352941176471, 0.9960784314, 0.3254901961, 0.0039215686, 0.592156862745098, 0.9960784314, 0.3411764706, 0.0039215686, 0.596078431372549, 0.9960784314, 0.3568627451, 0.0039215686, 0.6, 0.9960784314, 0.3725490196, 0.0039215686, 0.6039215686274509, 0.9960784314, 0.3882352941, 0.0039215686, 0.6078431372549019, 0.9960784314, 0.4039215686, 0.0039215686, 0.611764705882353, 0.9960784314, 0.4196078431, 0.0039215686, 0.615686274509804, 0.9960784314, 0.4352941176, 0.0039215686, 0.6196078431372549, 0.9960784314, 0.4509803922, 0.0039215686, 0.6235294117647059, 0.9960784314, 0.4666666667, 0.0039215686, 0.6274509803921569, 0.9960784314, 0.4823529412, 0.0039215686, 0.6313725490196078, 0.9960784314, 0.4980392157, 0.0039215686, 0.6352941176470588, 0.9960784314, 0.5137254902, 0.0039215686, 0.6392156862745098, 0.9960784314, 0.5294117647, 0.0039215686, 0.6431372549019608, 0.9960784314, 0.5450980392, 0.0039215686, 0.6470588235294118, 0.9960784314, 0.5607843137, 0.0039215686, 0.6509803921568628, 0.9960784314, 0.5764705882, 0.0039215686, 0.6549019607843137, 0.9960784314, 0.5921568627, 0.0039215686, 0.6588235294117647, 0.9960784314, 0.6078431373, 0.0039215686, 0.6627450980392157, 0.9960784314, 0.6235294118, 0.0039215686, 0.6666666666666666, 0.9960784314, 0.6392156863, 0.0039215686, 0.6705882352941176, 0.9960784314, 0.6549019608, 0.0039215686, 0.6745098039215687, 0.9960784314, 0.6705882353, 0.0039215686, 0.6784313725490196, 0.9960784314, 0.6862745098, 0.0039215686, 0.6823529411764706, 0.9960784314, 0.7019607843, 0.0039215686, 0.6862745098039216, 0.9960784314, 0.7176470588, 0.0039215686, 0.6901960784313725, 0.9960784314, 0.7333333333, 0.0039215686, 0.6941176470588235, 0.9960784314, 0.7490196078, 0.0039215686, 0.6980392156862745, 0.9960784314, 0.7607843137, 0.0039215686, 0.7019607843137254, 0.9960784314, 0.7764705882, 0.0039215686, 0.7058823529411765, 0.9960784314, 0.7921568627, 0.0039215686, 0.7098039215686275, 0.9960784314, 0.8078431373, 0.0039215686, 0.7137254901960784, 0.9960784314, 0.8235294118, 0.0039215686, 0.7176470588235294, 0.9960784314, 0.8392156863, 0.0039215686, 0.7215686274509804, 0.9960784314, 0.8549019608, 0.0039215686, 0.7254901960784313, 0.9960784314, 0.8705882353, 0.0039215686, 0.7294117647058823, 0.9960784314, 0.8862745098, 0.0039215686, 0.7333333333333333, 0.9960784314, 0.9019607843, 0.0039215686, 0.7372549019607844, 0.9960784314, 0.9176470588, 0.0039215686, 0.7411764705882353, 0.9960784314, 0.9333333333, 0.0039215686, 0.7450980392156863, 0.9960784314, 0.9490196078, 0.0039215686, 0.7490196078431373, 0.9960784314, 0.9647058824, 0.0039215686, 0.7529411764705882, 0.9960784314, 0.9803921569, 0.0039215686, 0.7568627450980392, 0.9960784314, 0.9960784314, 0.0039215686, 0.7607843137254902, 0.9960784314, 0.9960784314, 0.0196078431, 0.7647058823529411, 0.9960784314, 0.9960784314, 0.0352941176, 0.7686274509803922, 0.9960784314, 0.9960784314, 0.0509803922, 0.7725490196078432, 0.9960784314, 0.9960784314, 0.0666666667, 0.7764705882352941, 0.9960784314, 0.9960784314, 0.0823529412, 0.7803921568627451, 0.9960784314, 0.9960784314, 0.0980392157, 0.7843137254901961, 0.9960784314, 0.9960784314, 0.1137254902, 0.788235294117647, 0.9960784314, 0.9960784314, 0.1294117647, 0.792156862745098, 0.9960784314, 0.9960784314, 0.1450980392, 0.796078431372549, 0.9960784314, 0.9960784314, 0.1607843137, 0.8, 0.9960784314, 0.9960784314, 0.1764705882, 0.803921568627451, 0.9960784314, 0.9960784314, 0.1921568627, 0.807843137254902, 0.9960784314, 0.9960784314, 0.2078431373, 0.8117647058823529, 0.9960784314, 0.9960784314, 0.2235294118, 0.8156862745098039, 0.9960784314, 0.9960784314, 0.2392156863, 0.8196078431372549, 0.9960784314, 0.9960784314, 0.2509803922, 0.8235294117647058, 0.9960784314, 0.9960784314, 0.2666666667, 0.8274509803921568, 0.9960784314, 0.9960784314, 0.2823529412, 0.8313725490196079, 0.9960784314, 0.9960784314, 0.2980392157, 0.8352941176470589, 0.9960784314, 0.9960784314, 0.3137254902, 0.8392156862745098, 0.9960784314, 0.9960784314, 0.3333333333, 0.8431372549019608, 0.9960784314, 0.9960784314, 0.3490196078, 0.8470588235294118, 0.9960784314, 0.9960784314, 0.3647058824, 0.8509803921568627, 0.9960784314, 0.9960784314, 0.3803921569, 0.8549019607843137, 0.9960784314, 0.9960784314, 0.3960784314, 0.8588235294117647, 0.9960784314, 0.9960784314, 0.4117647059, 0.8627450980392157, 0.9960784314, 0.9960784314, 0.4274509804, 0.8666666666666667, 0.9960784314, 0.9960784314, 0.4431372549, 0.8705882352941177, 0.9960784314, 0.9960784314, 0.4588235294, 0.8745098039215686, 0.9960784314, 0.9960784314, 0.4745098039, 0.8784313725490196, 0.9960784314, 0.9960784314, 0.4901960784, 0.8823529411764706, 0.9960784314, 0.9960784314, 0.5058823529, 0.8862745098039215, 0.9960784314, 0.9960784314, 0.5215686275, 0.8901960784313725, 0.9960784314, 0.9960784314, 0.537254902, 0.8941176470588236, 0.9960784314, 0.9960784314, 0.5529411765, 0.8980392156862745, 0.9960784314, 0.9960784314, 0.568627451, 0.9019607843137255, 0.9960784314, 0.9960784314, 0.5843137255, 0.9058823529411765, 0.9960784314, 0.9960784314, 0.6, 0.9098039215686274, 0.9960784314, 0.9960784314, 0.6156862745, 0.9137254901960784, 0.9960784314, 0.9960784314, 0.631372549, 0.9176470588235294, 0.9960784314, 0.9960784314, 0.6470588235, 0.9215686274509803, 0.9960784314, 0.9960784314, 0.6666666667, 0.9254901960784314, 0.9960784314, 0.9960784314, 0.6823529412, 0.9294117647058824, 0.9960784314, 0.9960784314, 0.6980392157, 0.9333333333333333, 0.9960784314, 0.9960784314, 0.7137254902, 0.9372549019607843, 0.9960784314, 0.9960784314, 0.7294117647, 0.9411764705882354, 0.9960784314, 0.9960784314, 0.7450980392, 0.9450980392156864, 0.9960784314, 0.9960784314, 0.7568627451, 0.9490196078431372, 0.9960784314, 0.9960784314, 0.7725490196, 0.9529411764705882, 0.9960784314, 0.9960784314, 0.7882352941, 0.9568627450980394, 0.9960784314, 0.9960784314, 0.8039215686, 0.9607843137254903, 0.9960784314, 0.9960784314, 0.8196078431, 0.9647058823529413, 0.9960784314, 0.9960784314, 0.8352941176, 0.9686274509803922, 0.9960784314, 0.9960784314, 0.8509803922, 0.9725490196078431, 0.9960784314, 0.9960784314, 0.8666666667, 0.9764705882352941, 0.9960784314, 0.9960784314, 0.8823529412, 0.9803921568627451, 0.9960784314, 0.9960784314, 0.8980392157, 0.984313725490196, 0.9960784314, 0.9960784314, 0.9137254902, 0.9882352941176471, 0.9960784314, 0.9960784314, 0.9294117647, 0.9921568627450981, 0.9960784314, 0.9960784314, 0.9450980392, 0.996078431372549, 0.9960784314, 0.9960784314, 0.9607843137, 1.0, 0.9960784314, 0.9960784314, 0.9607843137]
}, {
  ColorSpace: 'RGB',
  Name: 'red_hot',
  RGBPoints: [0.0, 0.0, 0.0, 0.0, 0.00392156862745098, 0.0, 0.0, 0.0, 0.00784313725490196, 0.0, 0.0, 0.0, 0.011764705882352941, 0.0, 0.0, 0.0, 0.01568627450980392, 0.0039215686, 0.0039215686, 0.0039215686, 0.0196078431372549, 0.0039215686, 0.0039215686, 0.0039215686, 0.023529411764705882, 0.0039215686, 0.0039215686, 0.0039215686, 0.027450980392156862, 0.0039215686, 0.0039215686, 0.0039215686, 0.03137254901960784, 0.0039215686, 0.0039215686, 0.0039215686, 0.03529411764705882, 0.0156862745, 0.0, 0.0, 0.0392156862745098, 0.0274509804, 0.0, 0.0, 0.043137254901960784, 0.0392156863, 0.0, 0.0, 0.047058823529411764, 0.0509803922, 0.0, 0.0, 0.050980392156862744, 0.062745098, 0.0, 0.0, 0.054901960784313725, 0.0784313725, 0.0, 0.0, 0.05882352941176471, 0.0901960784, 0.0, 0.0, 0.06274509803921569, 0.1058823529, 0.0, 0.0, 0.06666666666666667, 0.1176470588, 0.0, 0.0, 0.07058823529411765, 0.1294117647, 0.0, 0.0, 0.07450980392156863, 0.1411764706, 0.0, 0.0, 0.0784313725490196, 0.1529411765, 0.0, 0.0, 0.08235294117647059, 0.1647058824, 0.0, 0.0, 0.08627450980392157, 0.1764705882, 0.0, 0.0, 0.09019607843137255, 0.1882352941, 0.0, 0.0, 0.09411764705882353, 0.2039215686, 0.0, 0.0, 0.09803921568627451, 0.2156862745, 0.0, 0.0, 0.10196078431372549, 0.2274509804, 0.0, 0.0, 0.10588235294117647, 0.2392156863, 0.0, 0.0, 0.10980392156862745, 0.2549019608, 0.0, 0.0, 0.11372549019607843, 0.2666666667, 0.0, 0.0, 0.11764705882352942, 0.2784313725, 0.0, 0.0, 0.12156862745098039, 0.2901960784, 0.0, 0.0, 0.12549019607843137, 0.3058823529, 0.0, 0.0, 0.12941176470588237, 0.3176470588, 0.0, 0.0, 0.13333333333333333, 0.3294117647, 0.0, 0.0, 0.13725490196078433, 0.3411764706, 0.0, 0.0, 0.1411764705882353, 0.3529411765, 0.0, 0.0, 0.1450980392156863, 0.3647058824, 0.0, 0.0, 0.14901960784313725, 0.3764705882, 0.0, 0.0, 0.15294117647058825, 0.3882352941, 0.0, 0.0, 0.1568627450980392, 0.4039215686, 0.0, 0.0, 0.1607843137254902, 0.4156862745, 0.0, 0.0, 0.16470588235294117, 0.431372549, 0.0, 0.0, 0.16862745098039217, 0.4431372549, 0.0, 0.0, 0.17254901960784313, 0.4588235294, 0.0, 0.0, 0.17647058823529413, 0.4705882353, 0.0, 0.0, 0.1803921568627451, 0.4823529412, 0.0, 0.0, 0.1843137254901961, 0.4941176471, 0.0, 0.0, 0.18823529411764706, 0.5098039216, 0.0, 0.0, 0.19215686274509805, 0.5215686275, 0.0, 0.0, 0.19607843137254902, 0.5333333333, 0.0, 0.0, 0.2, 0.5450980392, 0.0, 0.0, 0.20392156862745098, 0.5568627451, 0.0, 0.0, 0.20784313725490197, 0.568627451, 0.0, 0.0, 0.21176470588235294, 0.5803921569, 0.0, 0.0, 0.21568627450980393, 0.5921568627, 0.0, 0.0, 0.2196078431372549, 0.6078431373, 0.0, 0.0, 0.2235294117647059, 0.6196078431, 0.0, 0.0, 0.22745098039215686, 0.631372549, 0.0, 0.0, 0.23137254901960785, 0.6431372549, 0.0, 0.0, 0.23529411764705885, 0.6588235294, 0.0, 0.0, 0.23921568627450984, 0.6705882353, 0.0, 0.0, 0.24313725490196078, 0.6823529412, 0.0, 0.0, 0.24705882352941178, 0.6941176471, 0.0, 0.0, 0.25098039215686274, 0.7098039216, 0.0, 0.0, 0.2549019607843137, 0.7215686275, 0.0, 0.0, 0.25882352941176473, 0.7333333333, 0.0, 0.0, 0.2627450980392157, 0.7450980392, 0.0, 0.0, 0.26666666666666666, 0.7568627451, 0.0, 0.0, 0.27058823529411763, 0.768627451, 0.0, 0.0, 0.27450980392156865, 0.7843137255, 0.0, 0.0, 0.2784313725490196, 0.7960784314, 0.0, 0.0, 0.2823529411764706, 0.8117647059, 0.0, 0.0, 0.28627450980392155, 0.8235294118, 0.0, 0.0, 0.2901960784313726, 0.8352941176, 0.0, 0.0, 0.29411764705882354, 0.8470588235, 0.0, 0.0, 0.2980392156862745, 0.862745098, 0.0, 0.0, 0.30196078431372547, 0.8745098039, 0.0, 0.0, 0.3058823529411765, 0.8862745098, 0.0, 0.0, 0.30980392156862746, 0.8980392157, 0.0, 0.0, 0.3137254901960784, 0.9137254902, 0.0, 0.0, 0.3176470588235294, 0.9254901961, 0.0, 0.0, 0.3215686274509804, 0.937254902, 0.0, 0.0, 0.3254901960784314, 0.9490196078, 0.0, 0.0, 0.32941176470588235, 0.9607843137, 0.0, 0.0, 0.3333333333333333, 0.968627451, 0.0, 0.0, 0.33725490196078434, 0.9803921569, 0.0039215686, 0.0, 0.3411764705882353, 0.9882352941, 0.0078431373, 0.0, 0.34509803921568627, 1.0, 0.0117647059, 0.0, 0.34901960784313724, 1.0, 0.0235294118, 0.0, 0.35294117647058826, 1.0, 0.0352941176, 0.0, 0.3568627450980392, 1.0, 0.0470588235, 0.0, 0.3607843137254902, 1.0, 0.062745098, 0.0, 0.36470588235294116, 1.0, 0.0745098039, 0.0, 0.3686274509803922, 1.0, 0.0862745098, 0.0, 0.37254901960784315, 1.0, 0.0980392157, 0.0, 0.3764705882352941, 1.0, 0.1137254902, 0.0, 0.3803921568627451, 1.0, 0.1254901961, 0.0, 0.3843137254901961, 1.0, 0.137254902, 0.0, 0.38823529411764707, 1.0, 0.1490196078, 0.0, 0.39215686274509803, 1.0, 0.1647058824, 0.0, 0.396078431372549, 1.0, 0.1764705882, 0.0, 0.4, 1.0, 0.1882352941, 0.0, 0.403921568627451, 1.0, 0.2, 0.0, 0.40784313725490196, 1.0, 0.2156862745, 0.0, 0.4117647058823529, 1.0, 0.2274509804, 0.0, 0.41568627450980394, 1.0, 0.2392156863, 0.0, 0.4196078431372549, 1.0, 0.2509803922, 0.0, 0.4235294117647059, 1.0, 0.2666666667, 0.0, 0.42745098039215684, 1.0, 0.2784313725, 0.0, 0.43137254901960786, 1.0, 0.2901960784, 0.0, 0.43529411764705883, 1.0, 0.3019607843, 0.0, 0.4392156862745098, 1.0, 0.3176470588, 0.0, 0.44313725490196076, 1.0, 0.3294117647, 0.0, 0.4470588235294118, 1.0, 0.3411764706, 0.0, 0.45098039215686275, 1.0, 0.3529411765, 0.0, 0.4549019607843137, 1.0, 0.368627451, 0.0, 0.4588235294117647, 1.0, 0.3803921569, 0.0, 0.4627450980392157, 1.0, 0.3921568627, 0.0, 0.4666666666666667, 1.0, 0.4039215686, 0.0, 0.4705882352941177, 1.0, 0.4156862745, 0.0, 0.4745098039215686, 1.0, 0.4274509804, 0.0, 0.4784313725490197, 1.0, 0.4392156863, 0.0, 0.48235294117647065, 1.0, 0.4509803922, 0.0, 0.48627450980392156, 1.0, 0.4666666667, 0.0, 0.49019607843137253, 1.0, 0.4784313725, 0.0, 0.49411764705882355, 1.0, 0.4941176471, 0.0, 0.4980392156862745, 1.0, 0.5058823529, 0.0, 0.5019607843137255, 1.0, 0.5215686275, 0.0, 0.5058823529411764, 1.0, 0.5333333333, 0.0, 0.5098039215686274, 1.0, 0.5450980392, 0.0, 0.5137254901960784, 1.0, 0.5568627451, 0.0, 0.5176470588235295, 1.0, 0.568627451, 0.0, 0.5215686274509804, 1.0, 0.5803921569, 0.0, 0.5254901960784314, 1.0, 0.5921568627, 0.0, 0.5294117647058824, 1.0, 0.6039215686, 0.0, 0.5333333333333333, 1.0, 0.6196078431, 0.0, 0.5372549019607843, 1.0, 0.631372549, 0.0, 0.5411764705882353, 1.0, 0.6431372549, 0.0, 0.5450980392156862, 1.0, 0.6549019608, 0.0, 0.5490196078431373, 1.0, 0.6705882353, 0.0, 0.5529411764705883, 1.0, 0.6823529412, 0.0, 0.5568627450980392, 1.0, 0.6941176471, 0.0, 0.5607843137254902, 1.0, 0.7058823529, 0.0, 0.5647058823529412, 1.0, 0.7215686275, 0.0, 0.5686274509803921, 1.0, 0.7333333333, 0.0, 0.5725490196078431, 1.0, 0.7450980392, 0.0, 0.5764705882352941, 1.0, 0.7568627451, 0.0, 0.5803921568627451, 1.0, 0.7725490196, 0.0, 0.5843137254901961, 1.0, 0.7843137255, 0.0, 0.5882352941176471, 1.0, 0.7960784314, 0.0, 0.592156862745098, 1.0, 0.8078431373, 0.0, 0.596078431372549, 1.0, 0.8196078431, 0.0, 0.6, 1.0, 0.831372549, 0.0, 0.6039215686274509, 1.0, 0.8470588235, 0.0, 0.6078431372549019, 1.0, 0.8588235294, 0.0, 0.611764705882353, 1.0, 0.8745098039, 0.0, 0.615686274509804, 1.0, 0.8862745098, 0.0, 0.6196078431372549, 1.0, 0.8980392157, 0.0, 0.6235294117647059, 1.0, 0.9098039216, 0.0, 0.6274509803921569, 1.0, 0.9254901961, 0.0, 0.6313725490196078, 1.0, 0.937254902, 0.0, 0.6352941176470588, 1.0, 0.9490196078, 0.0, 0.6392156862745098, 1.0, 0.9607843137, 0.0, 0.6431372549019608, 1.0, 0.9764705882, 0.0, 0.6470588235294118, 1.0, 0.9803921569, 0.0039215686, 0.6509803921568628, 1.0, 0.9882352941, 0.0117647059, 0.6549019607843137, 1.0, 0.9921568627, 0.0156862745, 0.6588235294117647, 1.0, 1.0, 0.0235294118, 0.6627450980392157, 1.0, 1.0, 0.0352941176, 0.6666666666666666, 1.0, 1.0, 0.0470588235, 0.6705882352941176, 1.0, 1.0, 0.0588235294, 0.6745098039215687, 1.0, 1.0, 0.0745098039, 0.6784313725490196, 1.0, 1.0, 0.0862745098, 0.6823529411764706, 1.0, 1.0, 0.0980392157, 0.6862745098039216, 1.0, 1.0, 0.1098039216, 0.6901960784313725, 1.0, 1.0, 0.1254901961, 0.6941176470588235, 1.0, 1.0, 0.137254902, 0.6980392156862745, 1.0, 1.0, 0.1490196078, 0.7019607843137254, 1.0, 1.0, 0.1607843137, 0.7058823529411765, 1.0, 1.0, 0.1764705882, 0.7098039215686275, 1.0, 1.0, 0.1882352941, 0.7137254901960784, 1.0, 1.0, 0.2, 0.7176470588235294, 1.0, 1.0, 0.2117647059, 0.7215686274509804, 1.0, 1.0, 0.2274509804, 0.7254901960784313, 1.0, 1.0, 0.2392156863, 0.7294117647058823, 1.0, 1.0, 0.2509803922, 0.7333333333333333, 1.0, 1.0, 0.262745098, 0.7372549019607844, 1.0, 1.0, 0.2784313725, 0.7411764705882353, 1.0, 1.0, 0.2901960784, 0.7450980392156863, 1.0, 1.0, 0.3019607843, 0.7490196078431373, 1.0, 1.0, 0.3137254902, 0.7529411764705882, 1.0, 1.0, 0.3294117647, 0.7568627450980392, 1.0, 1.0, 0.3411764706, 0.7607843137254902, 1.0, 1.0, 0.3529411765, 0.7647058823529411, 1.0, 1.0, 0.3647058824, 0.7686274509803922, 1.0, 1.0, 0.3803921569, 0.7725490196078432, 1.0, 1.0, 0.3921568627, 0.7764705882352941, 1.0, 1.0, 0.4039215686, 0.7803921568627451, 1.0, 1.0, 0.4156862745, 0.7843137254901961, 1.0, 1.0, 0.431372549, 0.788235294117647, 1.0, 1.0, 0.4431372549, 0.792156862745098, 1.0, 1.0, 0.4549019608, 0.796078431372549, 1.0, 1.0, 0.4666666667, 0.8, 1.0, 1.0, 0.4784313725, 0.803921568627451, 1.0, 1.0, 0.4901960784, 0.807843137254902, 1.0, 1.0, 0.5019607843, 0.8117647058823529, 1.0, 1.0, 0.5137254902, 0.8156862745098039, 1.0, 1.0, 0.5294117647, 0.8196078431372549, 1.0, 1.0, 0.5411764706, 0.8235294117647058, 1.0, 1.0, 0.5568627451, 0.8274509803921568, 1.0, 1.0, 0.568627451, 0.8313725490196079, 1.0, 1.0, 0.5843137255, 0.8352941176470589, 1.0, 1.0, 0.5960784314, 0.8392156862745098, 1.0, 1.0, 0.6078431373, 0.8431372549019608, 1.0, 1.0, 0.6196078431, 0.8470588235294118, 1.0, 1.0, 0.631372549, 0.8509803921568627, 1.0, 1.0, 0.6431372549, 0.8549019607843137, 1.0, 1.0, 0.6549019608, 0.8588235294117647, 1.0, 1.0, 0.6666666667, 0.8627450980392157, 1.0, 1.0, 0.6823529412, 0.8666666666666667, 1.0, 1.0, 0.6941176471, 0.8705882352941177, 1.0, 1.0, 0.7058823529, 0.8745098039215686, 1.0, 1.0, 0.7176470588, 0.8784313725490196, 1.0, 1.0, 0.7333333333, 0.8823529411764706, 1.0, 1.0, 0.7450980392, 0.8862745098039215, 1.0, 1.0, 0.7568627451, 0.8901960784313725, 1.0, 1.0, 0.768627451, 0.8941176470588236, 1.0, 1.0, 0.7843137255, 0.8980392156862745, 1.0, 1.0, 0.7960784314, 0.9019607843137255, 1.0, 1.0, 0.8078431373, 0.9058823529411765, 1.0, 1.0, 0.8196078431, 0.9098039215686274, 1.0, 1.0, 0.8352941176, 0.9137254901960784, 1.0, 1.0, 0.8470588235, 0.9176470588235294, 1.0, 1.0, 0.8588235294, 0.9215686274509803, 1.0, 1.0, 0.8705882353, 0.9254901960784314, 1.0, 1.0, 0.8823529412, 0.9294117647058824, 1.0, 1.0, 0.8941176471, 0.9333333333333333, 1.0, 1.0, 0.9098039216, 0.9372549019607843, 1.0, 1.0, 0.9215686275, 0.9411764705882354, 1.0, 1.0, 0.937254902, 0.9450980392156864, 1.0, 1.0, 0.9490196078, 0.9490196078431372, 1.0, 1.0, 0.9607843137, 0.9529411764705882, 1.0, 1.0, 0.9725490196, 0.9568627450980394, 1.0, 1.0, 0.9882352941, 0.9607843137254903, 1.0, 1.0, 0.9882352941, 0.9647058823529413, 1.0, 1.0, 0.9921568627, 0.9686274509803922, 1.0, 1.0, 0.9960784314, 0.9725490196078431, 1.0, 1.0, 1.0, 0.9764705882352941, 1.0, 1.0, 1.0, 0.9803921568627451, 1.0, 1.0, 1.0, 0.984313725490196, 1.0, 1.0, 1.0, 0.9882352941176471, 1.0, 1.0, 1.0, 0.9921568627450981, 1.0, 1.0, 1.0, 0.996078431372549, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0]
}, {
  ColorSpace: 'RGB',
  Name: 's_pet',
  RGBPoints: [0.0, 0.0156862745, 0.0039215686, 0.0156862745, 0.00392156862745098, 0.0156862745, 0.0039215686, 0.0156862745, 0.00784313725490196, 0.0274509804, 0.0039215686, 0.031372549, 0.011764705882352941, 0.0352941176, 0.0039215686, 0.0509803922, 0.01568627450980392, 0.0392156863, 0.0039215686, 0.0666666667, 0.0196078431372549, 0.0509803922, 0.0039215686, 0.0823529412, 0.023529411764705882, 0.062745098, 0.0039215686, 0.0980392157, 0.027450980392156862, 0.0705882353, 0.0039215686, 0.1176470588, 0.03137254901960784, 0.0745098039, 0.0039215686, 0.1333333333, 0.03529411764705882, 0.0862745098, 0.0039215686, 0.1490196078, 0.0392156862745098, 0.0980392157, 0.0039215686, 0.1647058824, 0.043137254901960784, 0.1058823529, 0.0039215686, 0.1843137255, 0.047058823529411764, 0.1098039216, 0.0039215686, 0.2, 0.050980392156862744, 0.1215686275, 0.0039215686, 0.2156862745, 0.054901960784313725, 0.1333333333, 0.0039215686, 0.231372549, 0.05882352941176471, 0.137254902, 0.0039215686, 0.2509803922, 0.06274509803921569, 0.1490196078, 0.0039215686, 0.262745098, 0.06666666666666667, 0.1607843137, 0.0039215686, 0.2784313725, 0.07058823529411765, 0.168627451, 0.0039215686, 0.2941176471, 0.07450980392156863, 0.1725490196, 0.0039215686, 0.3137254902, 0.0784313725490196, 0.1843137255, 0.0039215686, 0.3294117647, 0.08235294117647059, 0.1960784314, 0.0039215686, 0.3450980392, 0.08627450980392157, 0.2039215686, 0.0039215686, 0.3607843137, 0.09019607843137255, 0.2078431373, 0.0039215686, 0.3803921569, 0.09411764705882353, 0.2196078431, 0.0039215686, 0.3960784314, 0.09803921568627451, 0.231372549, 0.0039215686, 0.4117647059, 0.10196078431372549, 0.2392156863, 0.0039215686, 0.4274509804, 0.10588235294117647, 0.2431372549, 0.0039215686, 0.4470588235, 0.10980392156862745, 0.2509803922, 0.0039215686, 0.462745098, 0.11372549019607843, 0.262745098, 0.0039215686, 0.4784313725, 0.11764705882352942, 0.2666666667, 0.0039215686, 0.4980392157, 0.12156862745098039, 0.2666666667, 0.0039215686, 0.4980392157, 0.12549019607843137, 0.262745098, 0.0039215686, 0.5137254902, 0.12941176470588237, 0.2509803922, 0.0039215686, 0.5294117647, 0.13333333333333333, 0.2431372549, 0.0039215686, 0.5450980392, 0.13725490196078433, 0.2392156863, 0.0039215686, 0.5607843137, 0.1411764705882353, 0.231372549, 0.0039215686, 0.5764705882, 0.1450980392156863, 0.2196078431, 0.0039215686, 0.5921568627, 0.14901960784313725, 0.2078431373, 0.0039215686, 0.6078431373, 0.15294117647058825, 0.2039215686, 0.0039215686, 0.6235294118, 0.1568627450980392, 0.1960784314, 0.0039215686, 0.6392156863, 0.1607843137254902, 0.1843137255, 0.0039215686, 0.6549019608, 0.16470588235294117, 0.1725490196, 0.0039215686, 0.6705882353, 0.16862745098039217, 0.168627451, 0.0039215686, 0.6862745098, 0.17254901960784313, 0.1607843137, 0.0039215686, 0.7019607843, 0.17647058823529413, 0.1490196078, 0.0039215686, 0.7176470588, 0.1803921568627451, 0.137254902, 0.0039215686, 0.7333333333, 0.1843137254901961, 0.1333333333, 0.0039215686, 0.7490196078, 0.18823529411764706, 0.1215686275, 0.0039215686, 0.7607843137, 0.19215686274509805, 0.1098039216, 0.0039215686, 0.7764705882, 0.19607843137254902, 0.1058823529, 0.0039215686, 0.7921568627, 0.2, 0.0980392157, 0.0039215686, 0.8078431373, 0.20392156862745098, 0.0862745098, 0.0039215686, 0.8235294118, 0.20784313725490197, 0.0745098039, 0.0039215686, 0.8392156863, 0.21176470588235294, 0.0705882353, 0.0039215686, 0.8549019608, 0.21568627450980393, 0.062745098, 0.0039215686, 0.8705882353, 0.2196078431372549, 0.0509803922, 0.0039215686, 0.8862745098, 0.2235294117647059, 0.0392156863, 0.0039215686, 0.9019607843, 0.22745098039215686, 0.0352941176, 0.0039215686, 0.9176470588, 0.23137254901960785, 0.0274509804, 0.0039215686, 0.9333333333, 0.23529411764705885, 0.0156862745, 0.0039215686, 0.9490196078, 0.23921568627450984, 0.0078431373, 0.0039215686, 0.9647058824, 0.24313725490196078, 0.0039215686, 0.0039215686, 0.9960784314, 0.24705882352941178, 0.0039215686, 0.0039215686, 0.9960784314, 0.25098039215686274, 0.0039215686, 0.0196078431, 0.9647058824, 0.2549019607843137, 0.0039215686, 0.0392156863, 0.9490196078, 0.25882352941176473, 0.0039215686, 0.0549019608, 0.9333333333, 0.2627450980392157, 0.0039215686, 0.0745098039, 0.9176470588, 0.26666666666666666, 0.0039215686, 0.0901960784, 0.9019607843, 0.27058823529411763, 0.0039215686, 0.1098039216, 0.8862745098, 0.27450980392156865, 0.0039215686, 0.1254901961, 0.8705882353, 0.2784313725490196, 0.0039215686, 0.1450980392, 0.8549019608, 0.2823529411764706, 0.0039215686, 0.1607843137, 0.8392156863, 0.28627450980392155, 0.0039215686, 0.1803921569, 0.8235294118, 0.2901960784313726, 0.0039215686, 0.1960784314, 0.8078431373, 0.29411764705882354, 0.0039215686, 0.2156862745, 0.7921568627, 0.2980392156862745, 0.0039215686, 0.231372549, 0.7764705882, 0.30196078431372547, 0.0039215686, 0.2509803922, 0.7607843137, 0.3058823529411765, 0.0039215686, 0.262745098, 0.7490196078, 0.30980392156862746, 0.0039215686, 0.2823529412, 0.7333333333, 0.3137254901960784, 0.0039215686, 0.2980392157, 0.7176470588, 0.3176470588235294, 0.0039215686, 0.3176470588, 0.7019607843, 0.3215686274509804, 0.0039215686, 0.3333333333, 0.6862745098, 0.3254901960784314, 0.0039215686, 0.3529411765, 0.6705882353, 0.32941176470588235, 0.0039215686, 0.368627451, 0.6549019608, 0.3333333333333333, 0.0039215686, 0.3882352941, 0.6392156863, 0.33725490196078434, 0.0039215686, 0.4039215686, 0.6235294118, 0.3411764705882353, 0.0039215686, 0.4235294118, 0.6078431373, 0.34509803921568627, 0.0039215686, 0.4392156863, 0.5921568627, 0.34901960784313724, 0.0039215686, 0.4588235294, 0.5764705882, 0.35294117647058826, 0.0039215686, 0.4745098039, 0.5607843137, 0.3568627450980392, 0.0039215686, 0.4941176471, 0.5450980392, 0.3607843137254902, 0.0039215686, 0.5098039216, 0.5294117647, 0.36470588235294116, 0.0039215686, 0.5294117647, 0.5137254902, 0.3686274509803922, 0.0039215686, 0.5450980392, 0.4980392157, 0.37254901960784315, 0.0039215686, 0.5647058824, 0.4784313725, 0.3764705882352941, 0.0039215686, 0.5803921569, 0.462745098, 0.3803921568627451, 0.0039215686, 0.6, 0.4470588235, 0.3843137254901961, 0.0039215686, 0.6156862745, 0.4274509804, 0.38823529411764707, 0.0039215686, 0.6352941176, 0.4117647059, 0.39215686274509803, 0.0039215686, 0.6509803922, 0.3960784314, 0.396078431372549, 0.0039215686, 0.6705882353, 0.3803921569, 0.4, 0.0039215686, 0.6862745098, 0.3607843137, 0.403921568627451, 0.0039215686, 0.7058823529, 0.3450980392, 0.40784313725490196, 0.0039215686, 0.7215686275, 0.3294117647, 0.4117647058823529, 0.0039215686, 0.7411764706, 0.3137254902, 0.41568627450980394, 0.0039215686, 0.7529411765, 0.2941176471, 0.4196078431372549, 0.0039215686, 0.7960784314, 0.2784313725, 0.4235294117647059, 0.0039215686, 0.7960784314, 0.262745098, 0.42745098039215684, 0.0392156863, 0.8039215686, 0.2509803922, 0.43137254901960786, 0.0745098039, 0.8117647059, 0.231372549, 0.43529411764705883, 0.1098039216, 0.8196078431, 0.2156862745, 0.4392156862745098, 0.1450980392, 0.8274509804, 0.2, 0.44313725490196076, 0.1803921569, 0.8352941176, 0.1843137255, 0.4470588235294118, 0.2156862745, 0.8431372549, 0.1647058824, 0.45098039215686275, 0.2509803922, 0.8509803922, 0.1490196078, 0.4549019607843137, 0.2823529412, 0.8588235294, 0.1333333333, 0.4588235294117647, 0.3176470588, 0.8666666667, 0.1176470588, 0.4627450980392157, 0.3529411765, 0.8745098039, 0.0980392157, 0.4666666666666667, 0.3882352941, 0.8823529412, 0.0823529412, 0.4705882352941177, 0.4235294118, 0.8901960784, 0.0666666667, 0.4745098039215686, 0.4588235294, 0.8980392157, 0.0509803922, 0.4784313725490197, 0.4941176471, 0.9058823529, 0.0431372549, 0.48235294117647065, 0.5294117647, 0.9137254902, 0.031372549, 0.48627450980392156, 0.5647058824, 0.9215686275, 0.0196078431, 0.49019607843137253, 0.6, 0.9294117647, 0.0078431373, 0.49411764705882355, 0.6352941176, 0.937254902, 0.0039215686, 0.4980392156862745, 0.6705882353, 0.9450980392, 0.0039215686, 0.5019607843137255, 0.7058823529, 0.9490196078, 0.0039215686, 0.5058823529411764, 0.7411764706, 0.9568627451, 0.0039215686, 0.5098039215686274, 0.7725490196, 0.9607843137, 0.0039215686, 0.5137254901960784, 0.8078431373, 0.968627451, 0.0039215686, 0.5176470588235295, 0.8431372549, 0.9725490196, 0.0039215686, 0.5215686274509804, 0.8784313725, 0.9803921569, 0.0039215686, 0.5254901960784314, 0.9137254902, 0.9843137255, 0.0039215686, 0.5294117647058824, 0.9490196078, 0.9921568627, 0.0039215686, 0.5333333333333333, 0.9960784314, 0.9960784314, 0.0039215686, 0.5372549019607843, 0.9960784314, 0.9960784314, 0.0039215686, 0.5411764705882353, 0.9960784314, 0.9921568627, 0.0039215686, 0.5450980392156862, 0.9960784314, 0.9843137255, 0.0039215686, 0.5490196078431373, 0.9960784314, 0.9764705882, 0.0039215686, 0.5529411764705883, 0.9960784314, 0.968627451, 0.0039215686, 0.5568627450980392, 0.9960784314, 0.9607843137, 0.0039215686, 0.5607843137254902, 0.9960784314, 0.9529411765, 0.0039215686, 0.5647058823529412, 0.9960784314, 0.9450980392, 0.0039215686, 0.5686274509803921, 0.9960784314, 0.937254902, 0.0039215686, 0.5725490196078431, 0.9960784314, 0.9294117647, 0.0039215686, 0.5764705882352941, 0.9960784314, 0.9215686275, 0.0039215686, 0.5803921568627451, 0.9960784314, 0.9137254902, 0.0039215686, 0.5843137254901961, 0.9960784314, 0.9058823529, 0.0039215686, 0.5882352941176471, 0.9960784314, 0.8980392157, 0.0039215686, 0.592156862745098, 0.9960784314, 0.8901960784, 0.0039215686, 0.596078431372549, 0.9960784314, 0.8823529412, 0.0039215686, 0.6, 0.9960784314, 0.8745098039, 0.0039215686, 0.6039215686274509, 0.9960784314, 0.8666666667, 0.0039215686, 0.6078431372549019, 0.9960784314, 0.8588235294, 0.0039215686, 0.611764705882353, 0.9960784314, 0.8509803922, 0.0039215686, 0.615686274509804, 0.9960784314, 0.8431372549, 0.0039215686, 0.6196078431372549, 0.9960784314, 0.8352941176, 0.0039215686, 0.6235294117647059, 0.9960784314, 0.8274509804, 0.0039215686, 0.6274509803921569, 0.9960784314, 0.8196078431, 0.0039215686, 0.6313725490196078, 0.9960784314, 0.8117647059, 0.0039215686, 0.6352941176470588, 0.9960784314, 0.8039215686, 0.0039215686, 0.6392156862745098, 0.9960784314, 0.7960784314, 0.0039215686, 0.6431372549019608, 0.9960784314, 0.7882352941, 0.0039215686, 0.6470588235294118, 0.9960784314, 0.7803921569, 0.0039215686, 0.6509803921568628, 0.9960784314, 0.7725490196, 0.0039215686, 0.6549019607843137, 0.9960784314, 0.7647058824, 0.0039215686, 0.6588235294117647, 0.9960784314, 0.7568627451, 0.0039215686, 0.6627450980392157, 0.9960784314, 0.7490196078, 0.0039215686, 0.6666666666666666, 0.9960784314, 0.7450980392, 0.0039215686, 0.6705882352941176, 0.9960784314, 0.737254902, 0.0039215686, 0.6745098039215687, 0.9960784314, 0.7294117647, 0.0039215686, 0.6784313725490196, 0.9960784314, 0.7215686275, 0.0039215686, 0.6823529411764706, 0.9960784314, 0.7137254902, 0.0039215686, 0.6862745098039216, 0.9960784314, 0.7058823529, 0.0039215686, 0.6901960784313725, 0.9960784314, 0.6980392157, 0.0039215686, 0.6941176470588235, 0.9960784314, 0.6901960784, 0.0039215686, 0.6980392156862745, 0.9960784314, 0.6823529412, 0.0039215686, 0.7019607843137254, 0.9960784314, 0.6745098039, 0.0039215686, 0.7058823529411765, 0.9960784314, 0.6666666667, 0.0039215686, 0.7098039215686275, 0.9960784314, 0.6588235294, 0.0039215686, 0.7137254901960784, 0.9960784314, 0.6509803922, 0.0039215686, 0.7176470588235294, 0.9960784314, 0.6431372549, 0.0039215686, 0.7215686274509804, 0.9960784314, 0.6352941176, 0.0039215686, 0.7254901960784313, 0.9960784314, 0.6274509804, 0.0039215686, 0.7294117647058823, 0.9960784314, 0.6196078431, 0.0039215686, 0.7333333333333333, 0.9960784314, 0.6117647059, 0.0039215686, 0.7372549019607844, 0.9960784314, 0.6039215686, 0.0039215686, 0.7411764705882353, 0.9960784314, 0.5960784314, 0.0039215686, 0.7450980392156863, 0.9960784314, 0.5882352941, 0.0039215686, 0.7490196078431373, 0.9960784314, 0.5803921569, 0.0039215686, 0.7529411764705882, 0.9960784314, 0.5725490196, 0.0039215686, 0.7568627450980392, 0.9960784314, 0.5647058824, 0.0039215686, 0.7607843137254902, 0.9960784314, 0.5568627451, 0.0039215686, 0.7647058823529411, 0.9960784314, 0.5490196078, 0.0039215686, 0.7686274509803922, 0.9960784314, 0.5411764706, 0.0039215686, 0.7725490196078432, 0.9960784314, 0.5333333333, 0.0039215686, 0.7764705882352941, 0.9960784314, 0.5254901961, 0.0039215686, 0.7803921568627451, 0.9960784314, 0.5176470588, 0.0039215686, 0.7843137254901961, 0.9960784314, 0.5098039216, 0.0039215686, 0.788235294117647, 0.9960784314, 0.5019607843, 0.0039215686, 0.792156862745098, 0.9960784314, 0.4941176471, 0.0039215686, 0.796078431372549, 0.9960784314, 0.4862745098, 0.0039215686, 0.8, 0.9960784314, 0.4784313725, 0.0039215686, 0.803921568627451, 0.9960784314, 0.4705882353, 0.0039215686, 0.807843137254902, 0.9960784314, 0.462745098, 0.0039215686, 0.8117647058823529, 0.9960784314, 0.4549019608, 0.0039215686, 0.8156862745098039, 0.9960784314, 0.4470588235, 0.0039215686, 0.8196078431372549, 0.9960784314, 0.4392156863, 0.0039215686, 0.8235294117647058, 0.9960784314, 0.431372549, 0.0039215686, 0.8274509803921568, 0.9960784314, 0.4235294118, 0.0039215686, 0.8313725490196079, 0.9960784314, 0.4156862745, 0.0039215686, 0.8352941176470589, 0.9960784314, 0.4078431373, 0.0039215686, 0.8392156862745098, 0.9960784314, 0.4, 0.0039215686, 0.8431372549019608, 0.9960784314, 0.3921568627, 0.0039215686, 0.8470588235294118, 0.9960784314, 0.3843137255, 0.0039215686, 0.8509803921568627, 0.9960784314, 0.3764705882, 0.0039215686, 0.8549019607843137, 0.9960784314, 0.368627451, 0.0039215686, 0.8588235294117647, 0.9960784314, 0.3607843137, 0.0039215686, 0.8627450980392157, 0.9960784314, 0.3529411765, 0.0039215686, 0.8666666666666667, 0.9960784314, 0.3450980392, 0.0039215686, 0.8705882352941177, 0.9960784314, 0.337254902, 0.0039215686, 0.8745098039215686, 0.9960784314, 0.3294117647, 0.0039215686, 0.8784313725490196, 0.9960784314, 0.3215686275, 0.0039215686, 0.8823529411764706, 0.9960784314, 0.3137254902, 0.0039215686, 0.8862745098039215, 0.9960784314, 0.3058823529, 0.0039215686, 0.8901960784313725, 0.9960784314, 0.2980392157, 0.0039215686, 0.8941176470588236, 0.9960784314, 0.2901960784, 0.0039215686, 0.8980392156862745, 0.9960784314, 0.2823529412, 0.0039215686, 0.9019607843137255, 0.9960784314, 0.2705882353, 0.0039215686, 0.9058823529411765, 0.9960784314, 0.2588235294, 0.0039215686, 0.9098039215686274, 0.9960784314, 0.2509803922, 0.0039215686, 0.9137254901960784, 0.9960784314, 0.2431372549, 0.0039215686, 0.9176470588235294, 0.9960784314, 0.231372549, 0.0039215686, 0.9215686274509803, 0.9960784314, 0.2196078431, 0.0039215686, 0.9254901960784314, 0.9960784314, 0.2117647059, 0.0039215686, 0.9294117647058824, 0.9960784314, 0.2, 0.0039215686, 0.9333333333333333, 0.9960784314, 0.1882352941, 0.0039215686, 0.9372549019607843, 0.9960784314, 0.1764705882, 0.0039215686, 0.9411764705882354, 0.9960784314, 0.168627451, 0.0039215686, 0.9450980392156864, 0.9960784314, 0.1568627451, 0.0039215686, 0.9490196078431372, 0.9960784314, 0.1450980392, 0.0039215686, 0.9529411764705882, 0.9960784314, 0.1333333333, 0.0039215686, 0.9568627450980394, 0.9960784314, 0.1254901961, 0.0039215686, 0.9607843137254903, 0.9960784314, 0.1137254902, 0.0039215686, 0.9647058823529413, 0.9960784314, 0.1019607843, 0.0039215686, 0.9686274509803922, 0.9960784314, 0.0901960784, 0.0039215686, 0.9725490196078431, 0.9960784314, 0.0823529412, 0.0039215686, 0.9764705882352941, 0.9960784314, 0.0705882353, 0.0039215686, 0.9803921568627451, 0.9960784314, 0.0588235294, 0.0039215686, 0.984313725490196, 0.9960784314, 0.0470588235, 0.0039215686, 0.9882352941176471, 0.9960784314, 0.0392156863, 0.0039215686, 0.9921568627450981, 0.9960784314, 0.0274509804, 0.0039215686, 0.996078431372549, 0.9960784314, 0.0156862745, 0.0039215686, 1.0, 0.9960784314, 0.0156862745, 0.0039215686]
}, {
  ColorSpace: 'RGB',
  Name: 'perfusion',
  RGBPoints: [0.0, 0.0, 0.0, 0.0, 0.00392156862745098, 0.0078431373, 0.0235294118, 0.0235294118, 0.00784313725490196, 0.0078431373, 0.031372549, 0.0470588235, 0.011764705882352941, 0.0078431373, 0.0392156863, 0.062745098, 0.01568627450980392, 0.0078431373, 0.0470588235, 0.0862745098, 0.0196078431372549, 0.0078431373, 0.0549019608, 0.1019607843, 0.023529411764705882, 0.0078431373, 0.0549019608, 0.1254901961, 0.027450980392156862, 0.0078431373, 0.062745098, 0.1411764706, 0.03137254901960784, 0.0078431373, 0.0705882353, 0.1647058824, 0.03529411764705882, 0.0078431373, 0.0784313725, 0.1803921569, 0.0392156862745098, 0.0078431373, 0.0862745098, 0.2039215686, 0.043137254901960784, 0.0078431373, 0.0862745098, 0.2196078431, 0.047058823529411764, 0.0078431373, 0.0941176471, 0.2431372549, 0.050980392156862744, 0.0078431373, 0.1019607843, 0.2666666667, 0.054901960784313725, 0.0078431373, 0.1098039216, 0.2823529412, 0.05882352941176471, 0.0078431373, 0.1176470588, 0.3058823529, 0.06274509803921569, 0.0078431373, 0.1176470588, 0.3215686275, 0.06666666666666667, 0.0078431373, 0.1254901961, 0.3450980392, 0.07058823529411765, 0.0078431373, 0.1333333333, 0.3607843137, 0.07450980392156863, 0.0078431373, 0.1411764706, 0.3843137255, 0.0784313725490196, 0.0078431373, 0.1490196078, 0.4, 0.08235294117647059, 0.0078431373, 0.1490196078, 0.4235294118, 0.08627450980392157, 0.0078431373, 0.1568627451, 0.4392156863, 0.09019607843137255, 0.0078431373, 0.1647058824, 0.462745098, 0.09411764705882353, 0.0078431373, 0.1725490196, 0.4784313725, 0.09803921568627451, 0.0078431373, 0.1803921569, 0.5019607843, 0.10196078431372549, 0.0078431373, 0.1803921569, 0.5254901961, 0.10588235294117647, 0.0078431373, 0.1882352941, 0.5411764706, 0.10980392156862745, 0.0078431373, 0.1960784314, 0.5647058824, 0.11372549019607843, 0.0078431373, 0.2039215686, 0.5803921569, 0.11764705882352942, 0.0078431373, 0.2117647059, 0.6039215686, 0.12156862745098039, 0.0078431373, 0.2117647059, 0.6196078431, 0.12549019607843137, 0.0078431373, 0.2196078431, 0.6431372549, 0.12941176470588237, 0.0078431373, 0.2274509804, 0.6588235294, 0.13333333333333333, 0.0078431373, 0.2352941176, 0.6823529412, 0.13725490196078433, 0.0078431373, 0.2431372549, 0.6980392157, 0.1411764705882353, 0.0078431373, 0.2431372549, 0.7215686275, 0.1450980392156863, 0.0078431373, 0.2509803922, 0.737254902, 0.14901960784313725, 0.0078431373, 0.2588235294, 0.7607843137, 0.15294117647058825, 0.0078431373, 0.2666666667, 0.7843137255, 0.1568627450980392, 0.0078431373, 0.2745098039, 0.8, 0.1607843137254902, 0.0078431373, 0.2745098039, 0.8235294118, 0.16470588235294117, 0.0078431373, 0.2823529412, 0.8392156863, 0.16862745098039217, 0.0078431373, 0.2901960784, 0.862745098, 0.17254901960784313, 0.0078431373, 0.2980392157, 0.8784313725, 0.17647058823529413, 0.0078431373, 0.3058823529, 0.9019607843, 0.1803921568627451, 0.0078431373, 0.3058823529, 0.9176470588, 0.1843137254901961, 0.0078431373, 0.2980392157, 0.9411764706, 0.18823529411764706, 0.0078431373, 0.3058823529, 0.9568627451, 0.19215686274509805, 0.0078431373, 0.2980392157, 0.9803921569, 0.19607843137254902, 0.0078431373, 0.2980392157, 0.9882352941, 0.2, 0.0078431373, 0.2901960784, 0.9803921569, 0.20392156862745098, 0.0078431373, 0.2901960784, 0.9647058824, 0.20784313725490197, 0.0078431373, 0.2823529412, 0.9568627451, 0.21176470588235294, 0.0078431373, 0.2823529412, 0.9411764706, 0.21568627450980393, 0.0078431373, 0.2745098039, 0.9333333333, 0.2196078431372549, 0.0078431373, 0.2666666667, 0.9176470588, 0.2235294117647059, 0.0078431373, 0.2666666667, 0.9098039216, 0.22745098039215686, 0.0078431373, 0.2588235294, 0.9019607843, 0.23137254901960785, 0.0078431373, 0.2588235294, 0.8862745098, 0.23529411764705885, 0.0078431373, 0.2509803922, 0.8784313725, 0.23921568627450984, 0.0078431373, 0.2509803922, 0.862745098, 0.24313725490196078, 0.0078431373, 0.2431372549, 0.8549019608, 0.24705882352941178, 0.0078431373, 0.2352941176, 0.8392156863, 0.25098039215686274, 0.0078431373, 0.2352941176, 0.831372549, 0.2549019607843137, 0.0078431373, 0.2274509804, 0.8235294118, 0.25882352941176473, 0.0078431373, 0.2274509804, 0.8078431373, 0.2627450980392157, 0.0078431373, 0.2196078431, 0.8, 0.26666666666666666, 0.0078431373, 0.2196078431, 0.7843137255, 0.27058823529411763, 0.0078431373, 0.2117647059, 0.7764705882, 0.27450980392156865, 0.0078431373, 0.2039215686, 0.7607843137, 0.2784313725490196, 0.0078431373, 0.2039215686, 0.7529411765, 0.2823529411764706, 0.0078431373, 0.1960784314, 0.7450980392, 0.28627450980392155, 0.0078431373, 0.1960784314, 0.7294117647, 0.2901960784313726, 0.0078431373, 0.1882352941, 0.7215686275, 0.29411764705882354, 0.0078431373, 0.1882352941, 0.7058823529, 0.2980392156862745, 0.0078431373, 0.1803921569, 0.6980392157, 0.30196078431372547, 0.0078431373, 0.1803921569, 0.6823529412, 0.3058823529411765, 0.0078431373, 0.1725490196, 0.6745098039, 0.30980392156862746, 0.0078431373, 0.1647058824, 0.6666666667, 0.3137254901960784, 0.0078431373, 0.1647058824, 0.6509803922, 0.3176470588235294, 0.0078431373, 0.1568627451, 0.6431372549, 0.3215686274509804, 0.0078431373, 0.1568627451, 0.6274509804, 0.3254901960784314, 0.0078431373, 0.1490196078, 0.6196078431, 0.32941176470588235, 0.0078431373, 0.1490196078, 0.6039215686, 0.3333333333333333, 0.0078431373, 0.1411764706, 0.5960784314, 0.33725490196078434, 0.0078431373, 0.1333333333, 0.5882352941, 0.3411764705882353, 0.0078431373, 0.1333333333, 0.5725490196, 0.34509803921568627, 0.0078431373, 0.1254901961, 0.5647058824, 0.34901960784313724, 0.0078431373, 0.1254901961, 0.5490196078, 0.35294117647058826, 0.0078431373, 0.1176470588, 0.5411764706, 0.3568627450980392, 0.0078431373, 0.1176470588, 0.5254901961, 0.3607843137254902, 0.0078431373, 0.1098039216, 0.5176470588, 0.36470588235294116, 0.0078431373, 0.1019607843, 0.5098039216, 0.3686274509803922, 0.0078431373, 0.1019607843, 0.4941176471, 0.37254901960784315, 0.0078431373, 0.0941176471, 0.4862745098, 0.3764705882352941, 0.0078431373, 0.0941176471, 0.4705882353, 0.3803921568627451, 0.0078431373, 0.0862745098, 0.462745098, 0.3843137254901961, 0.0078431373, 0.0862745098, 0.4470588235, 0.38823529411764707, 0.0078431373, 0.0784313725, 0.4392156863, 0.39215686274509803, 0.0078431373, 0.0705882353, 0.431372549, 0.396078431372549, 0.0078431373, 0.0705882353, 0.4156862745, 0.4, 0.0078431373, 0.062745098, 0.4078431373, 0.403921568627451, 0.0078431373, 0.062745098, 0.3921568627, 0.40784313725490196, 0.0078431373, 0.0549019608, 0.3843137255, 0.4117647058823529, 0.0078431373, 0.0549019608, 0.368627451, 0.41568627450980394, 0.0078431373, 0.0470588235, 0.3607843137, 0.4196078431372549, 0.0078431373, 0.0470588235, 0.3529411765, 0.4235294117647059, 0.0078431373, 0.0392156863, 0.337254902, 0.42745098039215684, 0.0078431373, 0.031372549, 0.3294117647, 0.43137254901960786, 0.0078431373, 0.031372549, 0.3137254902, 0.43529411764705883, 0.0078431373, 0.0235294118, 0.3058823529, 0.4392156862745098, 0.0078431373, 0.0235294118, 0.2901960784, 0.44313725490196076, 0.0078431373, 0.0156862745, 0.2823529412, 0.4470588235294118, 0.0078431373, 0.0156862745, 0.2745098039, 0.45098039215686275, 0.0078431373, 0.0078431373, 0.2588235294, 0.4549019607843137, 0.0235294118, 0.0078431373, 0.2509803922, 0.4588235294117647, 0.0078431373, 0.0078431373, 0.2352941176, 0.4627450980392157, 0.0078431373, 0.0078431373, 0.2274509804, 0.4666666666666667, 0.0078431373, 0.0078431373, 0.2117647059, 0.4705882352941177, 0.0078431373, 0.0078431373, 0.2039215686, 0.4745098039215686, 0.0078431373, 0.0078431373, 0.1960784314, 0.4784313725490197, 0.0078431373, 0.0078431373, 0.1803921569, 0.48235294117647065, 0.0078431373, 0.0078431373, 0.1725490196, 0.48627450980392156, 0.0078431373, 0.0078431373, 0.1568627451, 0.49019607843137253, 0.0078431373, 0.0078431373, 0.1490196078, 0.49411764705882355, 0.0078431373, 0.0078431373, 0.1333333333, 0.4980392156862745, 0.0078431373, 0.0078431373, 0.1254901961, 0.5019607843137255, 0.0078431373, 0.0078431373, 0.1176470588, 0.5058823529411764, 0.0078431373, 0.0078431373, 0.1019607843, 0.5098039215686274, 0.0078431373, 0.0078431373, 0.0941176471, 0.5137254901960784, 0.0078431373, 0.0078431373, 0.0784313725, 0.5176470588235295, 0.0078431373, 0.0078431373, 0.0705882353, 0.5215686274509804, 0.0078431373, 0.0078431373, 0.0549019608, 0.5254901960784314, 0.0078431373, 0.0078431373, 0.0470588235, 0.5294117647058824, 0.0235294118, 0.0078431373, 0.0392156863, 0.5333333333333333, 0.031372549, 0.0078431373, 0.0235294118, 0.5372549019607843, 0.0392156863, 0.0078431373, 0.0156862745, 0.5411764705882353, 0.0549019608, 0.0078431373, 0.0, 0.5450980392156862, 0.062745098, 0.0078431373, 0.0, 0.5490196078431373, 0.0705882353, 0.0078431373, 0.0, 0.5529411764705883, 0.0862745098, 0.0078431373, 0.0, 0.5568627450980392, 0.0941176471, 0.0078431373, 0.0, 0.5607843137254902, 0.1019607843, 0.0078431373, 0.0, 0.5647058823529412, 0.1098039216, 0.0078431373, 0.0, 0.5686274509803921, 0.1254901961, 0.0078431373, 0.0, 0.5725490196078431, 0.1333333333, 0.0078431373, 0.0, 0.5764705882352941, 0.1411764706, 0.0078431373, 0.0, 0.5803921568627451, 0.1568627451, 0.0078431373, 0.0, 0.5843137254901961, 0.1647058824, 0.0078431373, 0.0, 0.5882352941176471, 0.1725490196, 0.0078431373, 0.0, 0.592156862745098, 0.1882352941, 0.0078431373, 0.0, 0.596078431372549, 0.1960784314, 0.0078431373, 0.0, 0.6, 0.2039215686, 0.0078431373, 0.0, 0.6039215686274509, 0.2117647059, 0.0078431373, 0.0, 0.6078431372549019, 0.2274509804, 0.0078431373, 0.0, 0.611764705882353, 0.2352941176, 0.0078431373, 0.0, 0.615686274509804, 0.2431372549, 0.0078431373, 0.0, 0.6196078431372549, 0.2588235294, 0.0078431373, 0.0, 0.6235294117647059, 0.2666666667, 0.0078431373, 0.0, 0.6274509803921569, 0.2745098039, 0.0, 0.0, 0.6313725490196078, 0.2901960784, 0.0156862745, 0.0, 0.6352941176470588, 0.2980392157, 0.0235294118, 0.0, 0.6392156862745098, 0.3058823529, 0.0392156863, 0.0, 0.6431372549019608, 0.3137254902, 0.0470588235, 0.0, 0.6470588235294118, 0.3294117647, 0.0549019608, 0.0, 0.6509803921568628, 0.337254902, 0.0705882353, 0.0, 0.6549019607843137, 0.3450980392, 0.0784313725, 0.0, 0.6588235294117647, 0.3607843137, 0.0862745098, 0.0, 0.6627450980392157, 0.368627451, 0.1019607843, 0.0, 0.6666666666666666, 0.3764705882, 0.1098039216, 0.0, 0.6705882352941176, 0.3843137255, 0.1176470588, 0.0, 0.6745098039215687, 0.4, 0.1333333333, 0.0, 0.6784313725490196, 0.4078431373, 0.1411764706, 0.0, 0.6823529411764706, 0.4156862745, 0.1490196078, 0.0, 0.6862745098039216, 0.431372549, 0.1647058824, 0.0, 0.6901960784313725, 0.4392156863, 0.1725490196, 0.0, 0.6941176470588235, 0.4470588235, 0.1803921569, 0.0, 0.6980392156862745, 0.462745098, 0.1960784314, 0.0, 0.7019607843137254, 0.4705882353, 0.2039215686, 0.0, 0.7058823529411765, 0.4784313725, 0.2117647059, 0.0, 0.7098039215686275, 0.4862745098, 0.2274509804, 0.0, 0.7137254901960784, 0.5019607843, 0.2352941176, 0.0, 0.7176470588235294, 0.5098039216, 0.2431372549, 0.0, 0.7215686274509804, 0.5176470588, 0.2588235294, 0.0, 0.7254901960784313, 0.5333333333, 0.2666666667, 0.0, 0.7294117647058823, 0.5411764706, 0.2745098039, 0.0, 0.7333333333333333, 0.5490196078, 0.2901960784, 0.0, 0.7372549019607844, 0.5647058824, 0.2980392157, 0.0, 0.7411764705882353, 0.5725490196, 0.3058823529, 0.0, 0.7450980392156863, 0.5803921569, 0.3215686275, 0.0, 0.7490196078431373, 0.5882352941, 0.3294117647, 0.0, 0.7529411764705882, 0.6039215686, 0.337254902, 0.0, 0.7568627450980392, 0.6117647059, 0.3529411765, 0.0, 0.7607843137254902, 0.6196078431, 0.3607843137, 0.0, 0.7647058823529411, 0.6352941176, 0.368627451, 0.0, 0.7686274509803922, 0.6431372549, 0.3843137255, 0.0, 0.7725490196078432, 0.6509803922, 0.3921568627, 0.0, 0.7764705882352941, 0.6588235294, 0.4, 0.0, 0.7803921568627451, 0.6745098039, 0.4156862745, 0.0, 0.7843137254901961, 0.6823529412, 0.4235294118, 0.0, 0.788235294117647, 0.6901960784, 0.431372549, 0.0, 0.792156862745098, 0.7058823529, 0.4470588235, 0.0, 0.796078431372549, 0.7137254902, 0.4549019608, 0.0, 0.8, 0.7215686275, 0.462745098, 0.0, 0.803921568627451, 0.737254902, 0.4784313725, 0.0, 0.807843137254902, 0.7450980392, 0.4862745098, 0.0, 0.8117647058823529, 0.7529411765, 0.4941176471, 0.0, 0.8156862745098039, 0.7607843137, 0.5098039216, 0.0, 0.8196078431372549, 0.7764705882, 0.5176470588, 0.0, 0.8235294117647058, 0.7843137255, 0.5254901961, 0.0, 0.8274509803921568, 0.7921568627, 0.5411764706, 0.0, 0.8313725490196079, 0.8078431373, 0.5490196078, 0.0, 0.8352941176470589, 0.8156862745, 0.5568627451, 0.0, 0.8392156862745098, 0.8235294118, 0.5725490196, 0.0, 0.8431372549019608, 0.8392156863, 0.5803921569, 0.0, 0.8470588235294118, 0.8470588235, 0.5882352941, 0.0, 0.8509803921568627, 0.8549019608, 0.6039215686, 0.0, 0.8549019607843137, 0.862745098, 0.6117647059, 0.0, 0.8588235294117647, 0.8784313725, 0.6196078431, 0.0, 0.8627450980392157, 0.8862745098, 0.6352941176, 0.0, 0.8666666666666667, 0.8941176471, 0.6431372549, 0.0, 0.8705882352941177, 0.9098039216, 0.6509803922, 0.0, 0.8745098039215686, 0.9176470588, 0.6666666667, 0.0, 0.8784313725490196, 0.9254901961, 0.6745098039, 0.0, 0.8823529411764706, 0.9411764706, 0.6823529412, 0.0, 0.8862745098039215, 0.9490196078, 0.6980392157, 0.0, 0.8901960784313725, 0.9568627451, 0.7058823529, 0.0, 0.8941176470588236, 0.9647058824, 0.7137254902, 0.0, 0.8980392156862745, 0.9803921569, 0.7294117647, 0.0, 0.9019607843137255, 0.9882352941, 0.737254902, 0.0, 0.9058823529411765, 0.9960784314, 0.7450980392, 0.0, 0.9098039215686274, 0.9960784314, 0.7607843137, 0.0, 0.9137254901960784, 0.9960784314, 0.768627451, 0.0, 0.9176470588235294, 0.9960784314, 0.7764705882, 0.0, 0.9215686274509803, 0.9960784314, 0.7921568627, 0.0, 0.9254901960784314, 0.9960784314, 0.8, 0.0, 0.9294117647058824, 0.9960784314, 0.8078431373, 0.0, 0.9333333333333333, 0.9960784314, 0.8235294118, 0.0, 0.9372549019607843, 0.9960784314, 0.831372549, 0.0, 0.9411764705882354, 0.9960784314, 0.8392156863, 0.0, 0.9450980392156864, 0.9960784314, 0.8549019608, 0.0, 0.9490196078431372, 0.9960784314, 0.862745098, 0.0549019608, 0.9529411764705882, 0.9960784314, 0.8705882353, 0.1098039216, 0.9568627450980394, 0.9960784314, 0.8862745098, 0.1647058824, 0.9607843137254903, 0.9960784314, 0.8941176471, 0.2196078431, 0.9647058823529413, 0.9960784314, 0.9019607843, 0.2666666667, 0.9686274509803922, 0.9960784314, 0.9176470588, 0.3215686275, 0.9725490196078431, 0.9960784314, 0.9254901961, 0.3764705882, 0.9764705882352941, 0.9960784314, 0.9333333333, 0.431372549, 0.9803921568627451, 0.9960784314, 0.9490196078, 0.4862745098, 0.984313725490196, 0.9960784314, 0.9568627451, 0.5333333333, 0.9882352941176471, 0.9960784314, 0.9647058824, 0.5882352941, 0.9921568627450981, 0.9960784314, 0.9803921569, 0.6431372549, 0.996078431372549, 0.9960784314, 0.9882352941, 0.6980392157, 1.0, 0.9960784314, 0.9960784314, 0.7450980392]
}, {
  ColorSpace: 'RGB',
  Name: 'rainbow_2',
  RGBPoints: [0.0, 0.0, 0.0, 0.0, 0.00392156862745098, 0.0156862745, 0.0, 0.0117647059, 0.00784313725490196, 0.0352941176, 0.0, 0.0274509804, 0.011764705882352941, 0.0509803922, 0.0, 0.0392156863, 0.01568627450980392, 0.0705882353, 0.0, 0.0549019608, 0.0196078431372549, 0.0862745098, 0.0, 0.0745098039, 0.023529411764705882, 0.1058823529, 0.0, 0.0901960784, 0.027450980392156862, 0.1215686275, 0.0, 0.1098039216, 0.03137254901960784, 0.1411764706, 0.0, 0.1254901961, 0.03529411764705882, 0.1568627451, 0.0, 0.1490196078, 0.0392156862745098, 0.1764705882, 0.0, 0.168627451, 0.043137254901960784, 0.1960784314, 0.0, 0.1882352941, 0.047058823529411764, 0.2117647059, 0.0, 0.2078431373, 0.050980392156862744, 0.2274509804, 0.0, 0.231372549, 0.054901960784313725, 0.2392156863, 0.0, 0.2470588235, 0.05882352941176471, 0.2509803922, 0.0, 0.2666666667, 0.06274509803921569, 0.2666666667, 0.0, 0.2823529412, 0.06666666666666667, 0.2705882353, 0.0, 0.3019607843, 0.07058823529411765, 0.2823529412, 0.0, 0.3176470588, 0.07450980392156863, 0.2901960784, 0.0, 0.337254902, 0.0784313725490196, 0.3019607843, 0.0, 0.3568627451, 0.08235294117647059, 0.3098039216, 0.0, 0.3725490196, 0.08627450980392157, 0.3137254902, 0.0, 0.3921568627, 0.09019607843137255, 0.3215686275, 0.0, 0.4078431373, 0.09411764705882353, 0.3254901961, 0.0, 0.4274509804, 0.09803921568627451, 0.3333333333, 0.0, 0.4431372549, 0.10196078431372549, 0.3294117647, 0.0, 0.462745098, 0.10588235294117647, 0.337254902, 0.0, 0.4784313725, 0.10980392156862745, 0.3411764706, 0.0, 0.4980392157, 0.11372549019607843, 0.3450980392, 0.0, 0.5176470588, 0.11764705882352942, 0.337254902, 0.0, 0.5333333333, 0.12156862745098039, 0.3411764706, 0.0, 0.5529411765, 0.12549019607843137, 0.3411764706, 0.0, 0.568627451, 0.12941176470588237, 0.3411764706, 0.0, 0.5882352941, 0.13333333333333333, 0.3333333333, 0.0, 0.6039215686, 0.13725490196078433, 0.3294117647, 0.0, 0.6235294118, 0.1411764705882353, 0.3294117647, 0.0, 0.6392156863, 0.1450980392156863, 0.3294117647, 0.0, 0.6588235294, 0.14901960784313725, 0.3254901961, 0.0, 0.6784313725, 0.15294117647058825, 0.3098039216, 0.0, 0.6941176471, 0.1568627450980392, 0.3058823529, 0.0, 0.7137254902, 0.1607843137254902, 0.3019607843, 0.0, 0.7294117647, 0.16470588235294117, 0.2980392157, 0.0, 0.7490196078, 0.16862745098039217, 0.2784313725, 0.0, 0.7647058824, 0.17254901960784313, 0.2745098039, 0.0, 0.7843137255, 0.17647058823529413, 0.2666666667, 0.0, 0.8, 0.1803921568627451, 0.2588235294, 0.0, 0.8196078431, 0.1843137254901961, 0.2352941176, 0.0, 0.8392156863, 0.18823529411764706, 0.2274509804, 0.0, 0.8549019608, 0.19215686274509805, 0.2156862745, 0.0, 0.8745098039, 0.19607843137254902, 0.2078431373, 0.0, 0.8901960784, 0.2, 0.1803921569, 0.0, 0.9098039216, 0.20392156862745098, 0.168627451, 0.0, 0.9254901961, 0.20784313725490197, 0.1568627451, 0.0, 0.9450980392, 0.21176470588235294, 0.1411764706, 0.0, 0.9607843137, 0.21568627450980393, 0.1294117647, 0.0, 0.9803921569, 0.2196078431372549, 0.0980392157, 0.0, 1.0, 0.2235294117647059, 0.0823529412, 0.0, 1.0, 0.22745098039215686, 0.062745098, 0.0, 1.0, 0.23137254901960785, 0.0470588235, 0.0, 1.0, 0.23529411764705885, 0.0156862745, 0.0, 1.0, 0.23921568627450984, 0.0, 0.0, 1.0, 0.24313725490196078, 0.0, 0.0156862745, 1.0, 0.24705882352941178, 0.0, 0.031372549, 1.0, 0.25098039215686274, 0.0, 0.062745098, 1.0, 0.2549019607843137, 0.0, 0.0823529412, 1.0, 0.25882352941176473, 0.0, 0.0980392157, 1.0, 0.2627450980392157, 0.0, 0.1137254902, 1.0, 0.26666666666666666, 0.0, 0.1490196078, 1.0, 0.27058823529411763, 0.0, 0.1647058824, 1.0, 0.27450980392156865, 0.0, 0.1803921569, 1.0, 0.2784313725490196, 0.0, 0.2, 1.0, 0.2823529411764706, 0.0, 0.2156862745, 1.0, 0.28627450980392155, 0.0, 0.2470588235, 1.0, 0.2901960784313726, 0.0, 0.262745098, 1.0, 0.29411764705882354, 0.0, 0.2823529412, 1.0, 0.2980392156862745, 0.0, 0.2980392157, 1.0, 0.30196078431372547, 0.0, 0.3294117647, 1.0, 0.3058823529411765, 0.0, 0.3490196078, 1.0, 0.30980392156862746, 0.0, 0.3647058824, 1.0, 0.3137254901960784, 0.0, 0.3803921569, 1.0, 0.3176470588235294, 0.0, 0.4156862745, 1.0, 0.3215686274509804, 0.0, 0.431372549, 1.0, 0.3254901960784314, 0.0, 0.4470588235, 1.0, 0.32941176470588235, 0.0, 0.4666666667, 1.0, 0.3333333333333333, 0.0, 0.4980392157, 1.0, 0.33725490196078434, 0.0, 0.5137254902, 1.0, 0.3411764705882353, 0.0, 0.5294117647, 1.0, 0.34509803921568627, 0.0, 0.5490196078, 1.0, 0.34901960784313724, 0.0, 0.5647058824, 1.0, 0.35294117647058826, 0.0, 0.5960784314, 1.0, 0.3568627450980392, 0.0, 0.6156862745, 1.0, 0.3607843137254902, 0.0, 0.631372549, 1.0, 0.36470588235294116, 0.0, 0.6470588235, 1.0, 0.3686274509803922, 0.0, 0.6823529412, 1.0, 0.37254901960784315, 0.0, 0.6980392157, 1.0, 0.3764705882352941, 0.0, 0.7137254902, 1.0, 0.3803921568627451, 0.0, 0.7333333333, 1.0, 0.3843137254901961, 0.0, 0.7647058824, 1.0, 0.38823529411764707, 0.0, 0.7803921569, 1.0, 0.39215686274509803, 0.0, 0.7960784314, 1.0, 0.396078431372549, 0.0, 0.8156862745, 1.0, 0.4, 0.0, 0.8470588235, 1.0, 0.403921568627451, 0.0, 0.862745098, 1.0, 0.40784313725490196, 0.0, 0.8823529412, 1.0, 0.4117647058823529, 0.0, 0.8980392157, 1.0, 0.41568627450980394, 0.0, 0.9137254902, 1.0, 0.4196078431372549, 0.0, 0.9490196078, 1.0, 0.4235294117647059, 0.0, 0.9647058824, 1.0, 0.42745098039215684, 0.0, 0.9803921569, 1.0, 0.43137254901960786, 0.0, 1.0, 1.0, 0.43529411764705883, 0.0, 1.0, 0.9647058824, 0.4392156862745098, 0.0, 1.0, 0.9490196078, 0.44313725490196076, 0.0, 1.0, 0.9333333333, 0.4470588235294118, 0.0, 1.0, 0.9137254902, 0.45098039215686275, 0.0, 1.0, 0.8823529412, 0.4549019607843137, 0.0, 1.0, 0.862745098, 0.4588235294117647, 0.0, 1.0, 0.8470588235, 0.4627450980392157, 0.0, 1.0, 0.831372549, 0.4666666666666667, 0.0, 1.0, 0.7960784314, 0.4705882352941177, 0.0, 1.0, 0.7803921569, 0.4745098039215686, 0.0, 1.0, 0.7647058824, 0.4784313725490197, 0.0, 1.0, 0.7490196078, 0.48235294117647065, 0.0, 1.0, 0.7333333333, 0.48627450980392156, 0.0, 1.0, 0.6980392157, 0.49019607843137253, 0.0, 1.0, 0.6823529412, 0.49411764705882355, 0.0, 1.0, 0.6666666667, 0.4980392156862745, 0.0, 1.0, 0.6470588235, 0.5019607843137255, 0.0, 1.0, 0.6156862745, 0.5058823529411764, 0.0, 1.0, 0.5960784314, 0.5098039215686274, 0.0, 1.0, 0.5803921569, 0.5137254901960784, 0.0, 1.0, 0.5647058824, 0.5176470588235295, 0.0, 1.0, 0.5294117647, 0.5215686274509804, 0.0, 1.0, 0.5137254902, 0.5254901960784314, 0.0, 1.0, 0.4980392157, 0.5294117647058824, 0.0, 1.0, 0.4823529412, 0.5333333333333333, 0.0, 1.0, 0.4470588235, 0.5372549019607843, 0.0, 1.0, 0.431372549, 0.5411764705882353, 0.0, 1.0, 0.4156862745, 0.5450980392156862, 0.0, 1.0, 0.4, 0.5490196078431373, 0.0, 1.0, 0.3803921569, 0.5529411764705883, 0.0, 1.0, 0.3490196078, 0.5568627450980392, 0.0, 1.0, 0.3294117647, 0.5607843137254902, 0.0, 1.0, 0.3137254902, 0.5647058823529412, 0.0, 1.0, 0.2980392157, 0.5686274509803921, 0.0, 1.0, 0.262745098, 0.5725490196078431, 0.0, 1.0, 0.2470588235, 0.5764705882352941, 0.0, 1.0, 0.231372549, 0.5803921568627451, 0.0, 1.0, 0.2156862745, 0.5843137254901961, 0.0, 1.0, 0.1803921569, 0.5882352941176471, 0.0, 1.0, 0.1647058824, 0.592156862745098, 0.0, 1.0, 0.1490196078, 0.596078431372549, 0.0, 1.0, 0.1333333333, 0.6, 0.0, 1.0, 0.0980392157, 0.6039215686274509, 0.0, 1.0, 0.0823529412, 0.6078431372549019, 0.0, 1.0, 0.062745098, 0.611764705882353, 0.0, 1.0, 0.0470588235, 0.615686274509804, 0.0, 1.0, 0.031372549, 0.6196078431372549, 0.0, 1.0, 0.0, 0.6235294117647059, 0.0156862745, 1.0, 0.0, 0.6274509803921569, 0.031372549, 1.0, 0.0, 0.6313725490196078, 0.0470588235, 1.0, 0.0, 0.6352941176470588, 0.0823529412, 1.0, 0.0, 0.6392156862745098, 0.0980392157, 1.0, 0.0, 0.6431372549019608, 0.1137254902, 1.0, 0.0, 0.6470588235294118, 0.1294117647, 1.0, 0.0, 0.6509803921568628, 0.1647058824, 1.0, 0.0, 0.6549019607843137, 0.1803921569, 1.0, 0.0, 0.6588235294117647, 0.2, 1.0, 0.0, 0.6627450980392157, 0.2156862745, 1.0, 0.0, 0.6666666666666666, 0.2470588235, 1.0, 0.0, 0.6705882352941176, 0.262745098, 1.0, 0.0, 0.6745098039215687, 0.2823529412, 1.0, 0.0, 0.6784313725490196, 0.2980392157, 1.0, 0.0, 0.6823529411764706, 0.3137254902, 1.0, 0.0, 0.6862745098039216, 0.3490196078, 1.0, 0.0, 0.6901960784313725, 0.3647058824, 1.0, 0.0, 0.6941176470588235, 0.3803921569, 1.0, 0.0, 0.6980392156862745, 0.3960784314, 1.0, 0.0, 0.7019607843137254, 0.431372549, 1.0, 0.0, 0.7058823529411765, 0.4470588235, 1.0, 0.0, 0.7098039215686275, 0.4666666667, 1.0, 0.0, 0.7137254901960784, 0.4823529412, 1.0, 0.0, 0.7176470588235294, 0.5137254902, 1.0, 0.0, 0.7215686274509804, 0.5294117647, 1.0, 0.0, 0.7254901960784313, 0.5490196078, 1.0, 0.0, 0.7294117647058823, 0.5647058824, 1.0, 0.0, 0.7333333333333333, 0.6, 1.0, 0.0, 0.7372549019607844, 0.6156862745, 1.0, 0.0, 0.7411764705882353, 0.631372549, 1.0, 0.0, 0.7450980392156863, 0.6470588235, 1.0, 0.0, 0.7490196078431373, 0.662745098, 1.0, 0.0, 0.7529411764705882, 0.6980392157, 1.0, 0.0, 0.7568627450980392, 0.7137254902, 1.0, 0.0, 0.7607843137254902, 0.7333333333, 1.0, 0.0, 0.7647058823529411, 0.7490196078, 1.0, 0.0, 0.7686274509803922, 0.7803921569, 1.0, 0.0, 0.7725490196078432, 0.7960784314, 1.0, 0.0, 0.7764705882352941, 0.8156862745, 1.0, 0.0, 0.7803921568627451, 0.831372549, 1.0, 0.0, 0.7843137254901961, 0.8666666667, 1.0, 0.0, 0.788235294117647, 0.8823529412, 1.0, 0.0, 0.792156862745098, 0.8980392157, 1.0, 0.0, 0.796078431372549, 0.9137254902, 1.0, 0.0, 0.8, 0.9490196078, 1.0, 0.0, 0.803921568627451, 0.9647058824, 1.0, 0.0, 0.807843137254902, 0.9803921569, 1.0, 0.0, 0.8117647058823529, 1.0, 1.0, 0.0, 0.8156862745098039, 1.0, 0.9803921569, 0.0, 0.8196078431372549, 1.0, 0.9490196078, 0.0, 0.8235294117647058, 1.0, 0.9333333333, 0.0, 0.8274509803921568, 1.0, 0.9137254902, 0.0, 0.8313725490196079, 1.0, 0.8980392157, 0.0, 0.8352941176470589, 1.0, 0.8666666667, 0.0, 0.8392156862745098, 1.0, 0.8470588235, 0.0, 0.8431372549019608, 1.0, 0.831372549, 0.0, 0.8470588235294118, 1.0, 0.8156862745, 0.0, 0.8509803921568627, 1.0, 0.7803921569, 0.0, 0.8549019607843137, 1.0, 0.7647058824, 0.0, 0.8588235294117647, 1.0, 0.7490196078, 0.0, 0.8627450980392157, 1.0, 0.7333333333, 0.0, 0.8666666666666667, 1.0, 0.6980392157, 0.0, 0.8705882352941177, 1.0, 0.6823529412, 0.0, 0.8745098039215686, 1.0, 0.6666666667, 0.0, 0.8784313725490196, 1.0, 0.6470588235, 0.0, 0.8823529411764706, 1.0, 0.631372549, 0.0, 0.8862745098039215, 1.0, 0.6, 0.0, 0.8901960784313725, 1.0, 0.5803921569, 0.0, 0.8941176470588236, 1.0, 0.5647058824, 0.0, 0.8980392156862745, 1.0, 0.5490196078, 0.0, 0.9019607843137255, 1.0, 0.5137254902, 0.0, 0.9058823529411765, 1.0, 0.4980392157, 0.0, 0.9098039215686274, 1.0, 0.4823529412, 0.0, 0.9137254901960784, 1.0, 0.4666666667, 0.0, 0.9176470588235294, 1.0, 0.431372549, 0.0, 0.9215686274509803, 1.0, 0.4156862745, 0.0, 0.9254901960784314, 1.0, 0.4, 0.0, 0.9294117647058824, 1.0, 0.3803921569, 0.0, 0.9333333333333333, 1.0, 0.3490196078, 0.0, 0.9372549019607843, 1.0, 0.3333333333, 0.0, 0.9411764705882354, 1.0, 0.3137254902, 0.0, 0.9450980392156864, 1.0, 0.2980392157, 0.0, 0.9490196078431372, 1.0, 0.2823529412, 0.0, 0.9529411764705882, 1.0, 0.2470588235, 0.0, 0.9568627450980394, 1.0, 0.231372549, 0.0, 0.9607843137254903, 1.0, 0.2156862745, 0.0, 0.9647058823529413, 1.0, 0.2, 0.0, 0.9686274509803922, 1.0, 0.1647058824, 0.0, 0.9725490196078431, 1.0, 0.1490196078, 0.0, 0.9764705882352941, 1.0, 0.1333333333, 0.0, 0.9803921568627451, 1.0, 0.1137254902, 0.0, 0.984313725490196, 1.0, 0.0823529412, 0.0, 0.9882352941176471, 1.0, 0.0666666667, 0.0, 0.9921568627450981, 1.0, 0.0470588235, 0.0, 0.996078431372549, 1.0, 0.031372549, 0.0, 1.0, 1.0, 0.0, 0.0]
}, {
  ColorSpace: 'RGB',
  Name: 'suv',
  RGBPoints: [0.0, 1.0, 1.0, 1.0, 0.00392156862745098, 1.0, 1.0, 1.0, 0.00784313725490196, 1.0, 1.0, 1.0, 0.011764705882352941, 1.0, 1.0, 1.0, 0.01568627450980392, 1.0, 1.0, 1.0, 0.0196078431372549, 1.0, 1.0, 1.0, 0.023529411764705882, 1.0, 1.0, 1.0, 0.027450980392156862, 1.0, 1.0, 1.0, 0.03137254901960784, 1.0, 1.0, 1.0, 0.03529411764705882, 1.0, 1.0, 1.0, 0.0392156862745098, 1.0, 1.0, 1.0, 0.043137254901960784, 1.0, 1.0, 1.0, 0.047058823529411764, 1.0, 1.0, 1.0, 0.050980392156862744, 1.0, 1.0, 1.0, 0.054901960784313725, 1.0, 1.0, 1.0, 0.05882352941176471, 1.0, 1.0, 1.0, 0.06274509803921569, 1.0, 1.0, 1.0, 0.06666666666666667, 1.0, 1.0, 1.0, 0.07058823529411765, 1.0, 1.0, 1.0, 0.07450980392156863, 1.0, 1.0, 1.0, 0.0784313725490196, 1.0, 1.0, 1.0, 0.08235294117647059, 1.0, 1.0, 1.0, 0.08627450980392157, 1.0, 1.0, 1.0, 0.09019607843137255, 1.0, 1.0, 1.0, 0.09411764705882353, 1.0, 1.0, 1.0, 0.09803921568627451, 1.0, 1.0, 1.0, 0.10196078431372549, 0.737254902, 0.737254902, 0.737254902, 0.10588235294117647, 0.737254902, 0.737254902, 0.737254902, 0.10980392156862745, 0.737254902, 0.737254902, 0.737254902, 0.11372549019607843, 0.737254902, 0.737254902, 0.737254902, 0.11764705882352942, 0.737254902, 0.737254902, 0.737254902, 0.12156862745098039, 0.737254902, 0.737254902, 0.737254902, 0.12549019607843137, 0.737254902, 0.737254902, 0.737254902, 0.12941176470588237, 0.737254902, 0.737254902, 0.737254902, 0.13333333333333333, 0.737254902, 0.737254902, 0.737254902, 0.13725490196078433, 0.737254902, 0.737254902, 0.737254902, 0.1411764705882353, 0.737254902, 0.737254902, 0.737254902, 0.1450980392156863, 0.737254902, 0.737254902, 0.737254902, 0.14901960784313725, 0.737254902, 0.737254902, 0.737254902, 0.15294117647058825, 0.737254902, 0.737254902, 0.737254902, 0.1568627450980392, 0.737254902, 0.737254902, 0.737254902, 0.1607843137254902, 0.737254902, 0.737254902, 0.737254902, 0.16470588235294117, 0.737254902, 0.737254902, 0.737254902, 0.16862745098039217, 0.737254902, 0.737254902, 0.737254902, 0.17254901960784313, 0.737254902, 0.737254902, 0.737254902, 0.17647058823529413, 0.737254902, 0.737254902, 0.737254902, 0.1803921568627451, 0.737254902, 0.737254902, 0.737254902, 0.1843137254901961, 0.737254902, 0.737254902, 0.737254902, 0.18823529411764706, 0.737254902, 0.737254902, 0.737254902, 0.19215686274509805, 0.737254902, 0.737254902, 0.737254902, 0.19607843137254902, 0.737254902, 0.737254902, 0.737254902, 0.2, 0.737254902, 0.737254902, 0.737254902, 0.20392156862745098, 0.431372549, 0.0, 0.568627451, 0.20784313725490197, 0.431372549, 0.0, 0.568627451, 0.21176470588235294, 0.431372549, 0.0, 0.568627451, 0.21568627450980393, 0.431372549, 0.0, 0.568627451, 0.2196078431372549, 0.431372549, 0.0, 0.568627451, 0.2235294117647059, 0.431372549, 0.0, 0.568627451, 0.22745098039215686, 0.431372549, 0.0, 0.568627451, 0.23137254901960785, 0.431372549, 0.0, 0.568627451, 0.23529411764705885, 0.431372549, 0.0, 0.568627451, 0.23921568627450984, 0.431372549, 0.0, 0.568627451, 0.24313725490196078, 0.431372549, 0.0, 0.568627451, 0.24705882352941178, 0.431372549, 0.0, 0.568627451, 0.25098039215686274, 0.431372549, 0.0, 0.568627451, 0.2549019607843137, 0.431372549, 0.0, 0.568627451, 0.25882352941176473, 0.431372549, 0.0, 0.568627451, 0.2627450980392157, 0.431372549, 0.0, 0.568627451, 0.26666666666666666, 0.431372549, 0.0, 0.568627451, 0.27058823529411763, 0.431372549, 0.0, 0.568627451, 0.27450980392156865, 0.431372549, 0.0, 0.568627451, 0.2784313725490196, 0.431372549, 0.0, 0.568627451, 0.2823529411764706, 0.431372549, 0.0, 0.568627451, 0.28627450980392155, 0.431372549, 0.0, 0.568627451, 0.2901960784313726, 0.431372549, 0.0, 0.568627451, 0.29411764705882354, 0.431372549, 0.0, 0.568627451, 0.2980392156862745, 0.431372549, 0.0, 0.568627451, 0.30196078431372547, 0.431372549, 0.0, 0.568627451, 0.3058823529411765, 0.2509803922, 0.3333333333, 0.6509803922, 0.30980392156862746, 0.2509803922, 0.3333333333, 0.6509803922, 0.3137254901960784, 0.2509803922, 0.3333333333, 0.6509803922, 0.3176470588235294, 0.2509803922, 0.3333333333, 0.6509803922, 0.3215686274509804, 0.2509803922, 0.3333333333, 0.6509803922, 0.3254901960784314, 0.2509803922, 0.3333333333, 0.6509803922, 0.32941176470588235, 0.2509803922, 0.3333333333, 0.6509803922, 0.3333333333333333, 0.2509803922, 0.3333333333, 0.6509803922, 0.33725490196078434, 0.2509803922, 0.3333333333, 0.6509803922, 0.3411764705882353, 0.2509803922, 0.3333333333, 0.6509803922, 0.34509803921568627, 0.2509803922, 0.3333333333, 0.6509803922, 0.34901960784313724, 0.2509803922, 0.3333333333, 0.6509803922, 0.35294117647058826, 0.2509803922, 0.3333333333, 0.6509803922, 0.3568627450980392, 0.2509803922, 0.3333333333, 0.6509803922, 0.3607843137254902, 0.2509803922, 0.3333333333, 0.6509803922, 0.36470588235294116, 0.2509803922, 0.3333333333, 0.6509803922, 0.3686274509803922, 0.2509803922, 0.3333333333, 0.6509803922, 0.37254901960784315, 0.2509803922, 0.3333333333, 0.6509803922, 0.3764705882352941, 0.2509803922, 0.3333333333, 0.6509803922, 0.3803921568627451, 0.2509803922, 0.3333333333, 0.6509803922, 0.3843137254901961, 0.2509803922, 0.3333333333, 0.6509803922, 0.38823529411764707, 0.2509803922, 0.3333333333, 0.6509803922, 0.39215686274509803, 0.2509803922, 0.3333333333, 0.6509803922, 0.396078431372549, 0.2509803922, 0.3333333333, 0.6509803922, 0.4, 0.2509803922, 0.3333333333, 0.6509803922, 0.403921568627451, 0.2509803922, 0.3333333333, 0.6509803922, 0.40784313725490196, 0.0, 0.8, 1.0, 0.4117647058823529, 0.0, 0.8, 1.0, 0.41568627450980394, 0.0, 0.8, 1.0, 0.4196078431372549, 0.0, 0.8, 1.0, 0.4235294117647059, 0.0, 0.8, 1.0, 0.42745098039215684, 0.0, 0.8, 1.0, 0.43137254901960786, 0.0, 0.8, 1.0, 0.43529411764705883, 0.0, 0.8, 1.0, 0.4392156862745098, 0.0, 0.8, 1.0, 0.44313725490196076, 0.0, 0.8, 1.0, 0.4470588235294118, 0.0, 0.8, 1.0, 0.45098039215686275, 0.0, 0.8, 1.0, 0.4549019607843137, 0.0, 0.8, 1.0, 0.4588235294117647, 0.0, 0.8, 1.0, 0.4627450980392157, 0.0, 0.8, 1.0, 0.4666666666666667, 0.0, 0.8, 1.0, 0.4705882352941177, 0.0, 0.8, 1.0, 0.4745098039215686, 0.0, 0.8, 1.0, 0.4784313725490197, 0.0, 0.8, 1.0, 0.48235294117647065, 0.0, 0.8, 1.0, 0.48627450980392156, 0.0, 0.8, 1.0, 0.49019607843137253, 0.0, 0.8, 1.0, 0.49411764705882355, 0.0, 0.8, 1.0, 0.4980392156862745, 0.0, 0.8, 1.0, 0.5019607843137255, 0.0, 0.8, 1.0, 0.5058823529411764, 0.0, 0.6666666667, 0.5333333333, 0.5098039215686274, 0.0, 0.6666666667, 0.5333333333, 0.5137254901960784, 0.0, 0.6666666667, 0.5333333333, 0.5176470588235295, 0.0, 0.6666666667, 0.5333333333, 0.5215686274509804, 0.0, 0.6666666667, 0.5333333333, 0.5254901960784314, 0.0, 0.6666666667, 0.5333333333, 0.5294117647058824, 0.0, 0.6666666667, 0.5333333333, 0.5333333333333333, 0.0, 0.6666666667, 0.5333333333, 0.5372549019607843, 0.0, 0.6666666667, 0.5333333333, 0.5411764705882353, 0.0, 0.6666666667, 0.5333333333, 0.5450980392156862, 0.0, 0.6666666667, 0.5333333333, 0.5490196078431373, 0.0, 0.6666666667, 0.5333333333, 0.5529411764705883, 0.0, 0.6666666667, 0.5333333333, 0.5568627450980392, 0.0, 0.6666666667, 0.5333333333, 0.5607843137254902, 0.0, 0.6666666667, 0.5333333333, 0.5647058823529412, 0.0, 0.6666666667, 0.5333333333, 0.5686274509803921, 0.0, 0.6666666667, 0.5333333333, 0.5725490196078431, 0.0, 0.6666666667, 0.5333333333, 0.5764705882352941, 0.0, 0.6666666667, 0.5333333333, 0.5803921568627451, 0.0, 0.6666666667, 0.5333333333, 0.5843137254901961, 0.0, 0.6666666667, 0.5333333333, 0.5882352941176471, 0.0, 0.6666666667, 0.5333333333, 0.592156862745098, 0.0, 0.6666666667, 0.5333333333, 0.596078431372549, 0.0, 0.6666666667, 0.5333333333, 0.6, 0.0, 0.6666666667, 0.5333333333, 0.6039215686274509, 0.0, 0.6666666667, 0.5333333333, 0.6078431372549019, 0.4, 1.0, 0.4, 0.611764705882353, 0.4, 1.0, 0.4, 0.615686274509804, 0.4, 1.0, 0.4, 0.6196078431372549, 0.4, 1.0, 0.4, 0.6235294117647059, 0.4, 1.0, 0.4, 0.6274509803921569, 0.4, 1.0, 0.4, 0.6313725490196078, 0.4, 1.0, 0.4, 0.6352941176470588, 0.4, 1.0, 0.4, 0.6392156862745098, 0.4, 1.0, 0.4, 0.6431372549019608, 0.4, 1.0, 0.4, 0.6470588235294118, 0.4, 1.0, 0.4, 0.6509803921568628, 0.4, 1.0, 0.4, 0.6549019607843137, 0.4, 1.0, 0.4, 0.6588235294117647, 0.4, 1.0, 0.4, 0.6627450980392157, 0.4, 1.0, 0.4, 0.6666666666666666, 0.4, 1.0, 0.4, 0.6705882352941176, 0.4, 1.0, 0.4, 0.6745098039215687, 0.4, 1.0, 0.4, 0.6784313725490196, 0.4, 1.0, 0.4, 0.6823529411764706, 0.4, 1.0, 0.4, 0.6862745098039216, 0.4, 1.0, 0.4, 0.6901960784313725, 0.4, 1.0, 0.4, 0.6941176470588235, 0.4, 1.0, 0.4, 0.6980392156862745, 0.4, 1.0, 0.4, 0.7019607843137254, 0.4, 1.0, 0.4, 0.7058823529411765, 1.0, 0.9490196078, 0.0, 0.7098039215686275, 1.0, 0.9490196078, 0.0, 0.7137254901960784, 1.0, 0.9490196078, 0.0, 0.7176470588235294, 1.0, 0.9490196078, 0.0, 0.7215686274509804, 1.0, 0.9490196078, 0.0, 0.7254901960784313, 1.0, 0.9490196078, 0.0, 0.7294117647058823, 1.0, 0.9490196078, 0.0, 0.7333333333333333, 1.0, 0.9490196078, 0.0, 0.7372549019607844, 1.0, 0.9490196078, 0.0, 0.7411764705882353, 1.0, 0.9490196078, 0.0, 0.7450980392156863, 1.0, 0.9490196078, 0.0, 0.7490196078431373, 1.0, 0.9490196078, 0.0, 0.7529411764705882, 1.0, 0.9490196078, 0.0, 0.7568627450980392, 1.0, 0.9490196078, 0.0, 0.7607843137254902, 1.0, 0.9490196078, 0.0, 0.7647058823529411, 1.0, 0.9490196078, 0.0, 0.7686274509803922, 1.0, 0.9490196078, 0.0, 0.7725490196078432, 1.0, 0.9490196078, 0.0, 0.7764705882352941, 1.0, 0.9490196078, 0.0, 0.7803921568627451, 1.0, 0.9490196078, 0.0, 0.7843137254901961, 1.0, 0.9490196078, 0.0, 0.788235294117647, 1.0, 0.9490196078, 0.0, 0.792156862745098, 1.0, 0.9490196078, 0.0, 0.796078431372549, 1.0, 0.9490196078, 0.0, 0.8, 1.0, 0.9490196078, 0.0, 0.803921568627451, 1.0, 0.9490196078, 0.0, 0.807843137254902, 0.9490196078, 0.6509803922, 0.2509803922, 0.8117647058823529, 0.9490196078, 0.6509803922, 0.2509803922, 0.8156862745098039, 0.9490196078, 0.6509803922, 0.2509803922, 0.8196078431372549, 0.9490196078, 0.6509803922, 0.2509803922, 0.8235294117647058, 0.9490196078, 0.6509803922, 0.2509803922, 0.8274509803921568, 0.9490196078, 0.6509803922, 0.2509803922, 0.8313725490196079, 0.9490196078, 0.6509803922, 0.2509803922, 0.8352941176470589, 0.9490196078, 0.6509803922, 0.2509803922, 0.8392156862745098, 0.9490196078, 0.6509803922, 0.2509803922, 0.8431372549019608, 0.9490196078, 0.6509803922, 0.2509803922, 0.8470588235294118, 0.9490196078, 0.6509803922, 0.2509803922, 0.8509803921568627, 0.9490196078, 0.6509803922, 0.2509803922, 0.8549019607843137, 0.9490196078, 0.6509803922, 0.2509803922, 0.8588235294117647, 0.9490196078, 0.6509803922, 0.2509803922, 0.8627450980392157, 0.9490196078, 0.6509803922, 0.2509803922, 0.8666666666666667, 0.9490196078, 0.6509803922, 0.2509803922, 0.8705882352941177, 0.9490196078, 0.6509803922, 0.2509803922, 0.8745098039215686, 0.9490196078, 0.6509803922, 0.2509803922, 0.8784313725490196, 0.9490196078, 0.6509803922, 0.2509803922, 0.8823529411764706, 0.9490196078, 0.6509803922, 0.2509803922, 0.8862745098039215, 0.9490196078, 0.6509803922, 0.2509803922, 0.8901960784313725, 0.9490196078, 0.6509803922, 0.2509803922, 0.8941176470588236, 0.9490196078, 0.6509803922, 0.2509803922, 0.8980392156862745, 0.9490196078, 0.6509803922, 0.2509803922, 0.9019607843137255, 0.9490196078, 0.6509803922, 0.2509803922, 0.9058823529411765, 0.9490196078, 0.6509803922, 0.2509803922, 0.9098039215686274, 1.0, 0.0, 0.0, 0.9137254901960784, 1.0, 0.0, 0.0, 0.9176470588235294, 1.0, 0.0, 0.0, 0.9215686274509803, 1.0, 0.0, 0.0, 0.9254901960784314, 1.0, 0.0, 0.0, 0.9294117647058824, 1.0, 0.0, 0.0, 0.9333333333333333, 1.0, 0.0, 0.0, 0.9372549019607843, 1.0, 0.0, 0.0, 0.9411764705882354, 1.0, 0.0, 0.0, 0.9450980392156864, 1.0, 0.0, 0.0, 0.9490196078431372, 1.0, 0.0, 0.0, 0.9529411764705882, 1.0, 0.0, 0.0, 0.9568627450980394, 1.0, 0.0, 0.0, 0.9607843137254903, 1.0, 0.0, 0.0, 0.9647058823529413, 1.0, 0.0, 0.0, 0.9686274509803922, 1.0, 0.0, 0.0, 0.9725490196078431, 1.0, 0.0, 0.0, 0.9764705882352941, 1.0, 0.0, 0.0, 0.9803921568627451, 1.0, 0.0, 0.0, 0.984313725490196, 1.0, 0.0, 0.0, 0.9882352941176471, 1.0, 0.0, 0.0, 0.9921568627450981, 1.0, 0.0, 0.0, 0.996078431372549, 1.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0]
}, {
  ColorSpace: 'RGB',
  Name: 'ge_256',
  RGBPoints: [0.0, 0.0039215686, 0.0078431373, 0.0078431373, 0.00392156862745098, 0.0039215686, 0.0078431373, 0.0078431373, 0.00784313725490196, 0.0039215686, 0.0078431373, 0.0117647059, 0.011764705882352941, 0.0039215686, 0.0117647059, 0.0156862745, 0.01568627450980392, 0.0039215686, 0.0117647059, 0.0196078431, 0.0196078431372549, 0.0039215686, 0.0156862745, 0.0235294118, 0.023529411764705882, 0.0039215686, 0.0156862745, 0.0274509804, 0.027450980392156862, 0.0039215686, 0.0196078431, 0.031372549, 0.03137254901960784, 0.0039215686, 0.0196078431, 0.0352941176, 0.03529411764705882, 0.0039215686, 0.0235294118, 0.0392156863, 0.0392156862745098, 0.0039215686, 0.0235294118, 0.0431372549, 0.043137254901960784, 0.0039215686, 0.0274509804, 0.0470588235, 0.047058823529411764, 0.0039215686, 0.0274509804, 0.0509803922, 0.050980392156862744, 0.0039215686, 0.031372549, 0.0549019608, 0.054901960784313725, 0.0039215686, 0.031372549, 0.0588235294, 0.05882352941176471, 0.0039215686, 0.0352941176, 0.062745098, 0.06274509803921569, 0.0039215686, 0.0352941176, 0.0666666667, 0.06666666666666667, 0.0039215686, 0.0392156863, 0.0705882353, 0.07058823529411765, 0.0039215686, 0.0392156863, 0.0745098039, 0.07450980392156863, 0.0039215686, 0.0431372549, 0.0784313725, 0.0784313725490196, 0.0039215686, 0.0431372549, 0.0823529412, 0.08235294117647059, 0.0039215686, 0.0470588235, 0.0862745098, 0.08627450980392157, 0.0039215686, 0.0470588235, 0.0901960784, 0.09019607843137255, 0.0039215686, 0.0509803922, 0.0941176471, 0.09411764705882353, 0.0039215686, 0.0509803922, 0.0980392157, 0.09803921568627451, 0.0039215686, 0.0549019608, 0.1019607843, 0.10196078431372549, 0.0039215686, 0.0549019608, 0.1058823529, 0.10588235294117647, 0.0039215686, 0.0588235294, 0.1098039216, 0.10980392156862745, 0.0039215686, 0.0588235294, 0.1137254902, 0.11372549019607843, 0.0039215686, 0.062745098, 0.1176470588, 0.11764705882352942, 0.0039215686, 0.062745098, 0.1215686275, 0.12156862745098039, 0.0039215686, 0.0666666667, 0.1254901961, 0.12549019607843137, 0.0039215686, 0.0666666667, 0.1294117647, 0.12941176470588237, 0.0039215686, 0.0705882353, 0.1333333333, 0.13333333333333333, 0.0039215686, 0.0705882353, 0.137254902, 0.13725490196078433, 0.0039215686, 0.0745098039, 0.1411764706, 0.1411764705882353, 0.0039215686, 0.0745098039, 0.1450980392, 0.1450980392156863, 0.0039215686, 0.0784313725, 0.1490196078, 0.14901960784313725, 0.0039215686, 0.0784313725, 0.1529411765, 0.15294117647058825, 0.0039215686, 0.0823529412, 0.1568627451, 0.1568627450980392, 0.0039215686, 0.0823529412, 0.1607843137, 0.1607843137254902, 0.0039215686, 0.0862745098, 0.1647058824, 0.16470588235294117, 0.0039215686, 0.0862745098, 0.168627451, 0.16862745098039217, 0.0039215686, 0.0901960784, 0.1725490196, 0.17254901960784313, 0.0039215686, 0.0901960784, 0.1764705882, 0.17647058823529413, 0.0039215686, 0.0941176471, 0.1803921569, 0.1803921568627451, 0.0039215686, 0.0941176471, 0.1843137255, 0.1843137254901961, 0.0039215686, 0.0980392157, 0.1882352941, 0.18823529411764706, 0.0039215686, 0.0980392157, 0.1921568627, 0.19215686274509805, 0.0039215686, 0.1019607843, 0.1960784314, 0.19607843137254902, 0.0039215686, 0.1019607843, 0.2, 0.2, 0.0039215686, 0.1058823529, 0.2039215686, 0.20392156862745098, 0.0039215686, 0.1058823529, 0.2078431373, 0.20784313725490197, 0.0039215686, 0.1098039216, 0.2117647059, 0.21176470588235294, 0.0039215686, 0.1098039216, 0.2156862745, 0.21568627450980393, 0.0039215686, 0.1137254902, 0.2196078431, 0.2196078431372549, 0.0039215686, 0.1137254902, 0.2235294118, 0.2235294117647059, 0.0039215686, 0.1176470588, 0.2274509804, 0.22745098039215686, 0.0039215686, 0.1176470588, 0.231372549, 0.23137254901960785, 0.0039215686, 0.1215686275, 0.2352941176, 0.23529411764705885, 0.0039215686, 0.1215686275, 0.2392156863, 0.23921568627450984, 0.0039215686, 0.1254901961, 0.2431372549, 0.24313725490196078, 0.0039215686, 0.1254901961, 0.2470588235, 0.24705882352941178, 0.0039215686, 0.1294117647, 0.2509803922, 0.25098039215686274, 0.0039215686, 0.1294117647, 0.2509803922, 0.2549019607843137, 0.0078431373, 0.1254901961, 0.2549019608, 0.25882352941176473, 0.0156862745, 0.1254901961, 0.2588235294, 0.2627450980392157, 0.0235294118, 0.1215686275, 0.262745098, 0.26666666666666666, 0.031372549, 0.1215686275, 0.2666666667, 0.27058823529411763, 0.0392156863, 0.1176470588, 0.2705882353, 0.27450980392156865, 0.0470588235, 0.1176470588, 0.2745098039, 0.2784313725490196, 0.0549019608, 0.1137254902, 0.2784313725, 0.2823529411764706, 0.062745098, 0.1137254902, 0.2823529412, 0.28627450980392155, 0.0705882353, 0.1098039216, 0.2862745098, 0.2901960784313726, 0.0784313725, 0.1098039216, 0.2901960784, 0.29411764705882354, 0.0862745098, 0.1058823529, 0.2941176471, 0.2980392156862745, 0.0941176471, 0.1058823529, 0.2980392157, 0.30196078431372547, 0.1019607843, 0.1019607843, 0.3019607843, 0.3058823529411765, 0.1098039216, 0.1019607843, 0.3058823529, 0.30980392156862746, 0.1176470588, 0.0980392157, 0.3098039216, 0.3137254901960784, 0.1254901961, 0.0980392157, 0.3137254902, 0.3176470588235294, 0.1333333333, 0.0941176471, 0.3176470588, 0.3215686274509804, 0.1411764706, 0.0941176471, 0.3215686275, 0.3254901960784314, 0.1490196078, 0.0901960784, 0.3254901961, 0.32941176470588235, 0.1568627451, 0.0901960784, 0.3294117647, 0.3333333333333333, 0.1647058824, 0.0862745098, 0.3333333333, 0.33725490196078434, 0.1725490196, 0.0862745098, 0.337254902, 0.3411764705882353, 0.1803921569, 0.0823529412, 0.3411764706, 0.34509803921568627, 0.1882352941, 0.0823529412, 0.3450980392, 0.34901960784313724, 0.1960784314, 0.0784313725, 0.3490196078, 0.35294117647058826, 0.2039215686, 0.0784313725, 0.3529411765, 0.3568627450980392, 0.2117647059, 0.0745098039, 0.3568627451, 0.3607843137254902, 0.2196078431, 0.0745098039, 0.3607843137, 0.36470588235294116, 0.2274509804, 0.0705882353, 0.3647058824, 0.3686274509803922, 0.2352941176, 0.0705882353, 0.368627451, 0.37254901960784315, 0.2431372549, 0.0666666667, 0.3725490196, 0.3764705882352941, 0.2509803922, 0.0666666667, 0.3764705882, 0.3803921568627451, 0.2549019608, 0.062745098, 0.3803921569, 0.3843137254901961, 0.262745098, 0.062745098, 0.3843137255, 0.38823529411764707, 0.2705882353, 0.0588235294, 0.3882352941, 0.39215686274509803, 0.2784313725, 0.0588235294, 0.3921568627, 0.396078431372549, 0.2862745098, 0.0549019608, 0.3960784314, 0.4, 0.2941176471, 0.0549019608, 0.4, 0.403921568627451, 0.3019607843, 0.0509803922, 0.4039215686, 0.40784313725490196, 0.3098039216, 0.0509803922, 0.4078431373, 0.4117647058823529, 0.3176470588, 0.0470588235, 0.4117647059, 0.41568627450980394, 0.3254901961, 0.0470588235, 0.4156862745, 0.4196078431372549, 0.3333333333, 0.0431372549, 0.4196078431, 0.4235294117647059, 0.3411764706, 0.0431372549, 0.4235294118, 0.42745098039215684, 0.3490196078, 0.0392156863, 0.4274509804, 0.43137254901960786, 0.3568627451, 0.0392156863, 0.431372549, 0.43529411764705883, 0.3647058824, 0.0352941176, 0.4352941176, 0.4392156862745098, 0.3725490196, 0.0352941176, 0.4392156863, 0.44313725490196076, 0.3803921569, 0.031372549, 0.4431372549, 0.4470588235294118, 0.3882352941, 0.031372549, 0.4470588235, 0.45098039215686275, 0.3960784314, 0.0274509804, 0.4509803922, 0.4549019607843137, 0.4039215686, 0.0274509804, 0.4549019608, 0.4588235294117647, 0.4117647059, 0.0235294118, 0.4588235294, 0.4627450980392157, 0.4196078431, 0.0235294118, 0.462745098, 0.4666666666666667, 0.4274509804, 0.0196078431, 0.4666666667, 0.4705882352941177, 0.4352941176, 0.0196078431, 0.4705882353, 0.4745098039215686, 0.4431372549, 0.0156862745, 0.4745098039, 0.4784313725490197, 0.4509803922, 0.0156862745, 0.4784313725, 0.48235294117647065, 0.4588235294, 0.0117647059, 0.4823529412, 0.48627450980392156, 0.4666666667, 0.0117647059, 0.4862745098, 0.49019607843137253, 0.4745098039, 0.0078431373, 0.4901960784, 0.49411764705882355, 0.4823529412, 0.0078431373, 0.4941176471, 0.4980392156862745, 0.4901960784, 0.0039215686, 0.4980392157, 0.5019607843137255, 0.4980392157, 0.0117647059, 0.4980392157, 0.5058823529411764, 0.5058823529, 0.0156862745, 0.4901960784, 0.5098039215686274, 0.5137254902, 0.0235294118, 0.4823529412, 0.5137254901960784, 0.5215686275, 0.0274509804, 0.4745098039, 0.5176470588235295, 0.5294117647, 0.0352941176, 0.4666666667, 0.5215686274509804, 0.537254902, 0.0392156863, 0.4588235294, 0.5254901960784314, 0.5450980392, 0.0470588235, 0.4509803922, 0.5294117647058824, 0.5529411765, 0.0509803922, 0.4431372549, 0.5333333333333333, 0.5607843137, 0.0588235294, 0.4352941176, 0.5372549019607843, 0.568627451, 0.062745098, 0.4274509804, 0.5411764705882353, 0.5764705882, 0.0705882353, 0.4196078431, 0.5450980392156862, 0.5843137255, 0.0745098039, 0.4117647059, 0.5490196078431373, 0.5921568627, 0.0823529412, 0.4039215686, 0.5529411764705883, 0.6, 0.0862745098, 0.3960784314, 0.5568627450980392, 0.6078431373, 0.0941176471, 0.3882352941, 0.5607843137254902, 0.6156862745, 0.0980392157, 0.3803921569, 0.5647058823529412, 0.6235294118, 0.1058823529, 0.3725490196, 0.5686274509803921, 0.631372549, 0.1098039216, 0.3647058824, 0.5725490196078431, 0.6392156863, 0.1176470588, 0.3568627451, 0.5764705882352941, 0.6470588235, 0.1215686275, 0.3490196078, 0.5803921568627451, 0.6549019608, 0.1294117647, 0.3411764706, 0.5843137254901961, 0.662745098, 0.1333333333, 0.3333333333, 0.5882352941176471, 0.6705882353, 0.1411764706, 0.3254901961, 0.592156862745098, 0.6784313725, 0.1450980392, 0.3176470588, 0.596078431372549, 0.6862745098, 0.1529411765, 0.3098039216, 0.6, 0.6941176471, 0.1568627451, 0.3019607843, 0.6039215686274509, 0.7019607843, 0.1647058824, 0.2941176471, 0.6078431372549019, 0.7098039216, 0.168627451, 0.2862745098, 0.611764705882353, 0.7176470588, 0.1764705882, 0.2784313725, 0.615686274509804, 0.7254901961, 0.1803921569, 0.2705882353, 0.6196078431372549, 0.7333333333, 0.1882352941, 0.262745098, 0.6235294117647059, 0.7411764706, 0.1921568627, 0.2549019608, 0.6274509803921569, 0.7490196078, 0.2, 0.2509803922, 0.6313725490196078, 0.7529411765, 0.2039215686, 0.2431372549, 0.6352941176470588, 0.7607843137, 0.2117647059, 0.2352941176, 0.6392156862745098, 0.768627451, 0.2156862745, 0.2274509804, 0.6431372549019608, 0.7764705882, 0.2235294118, 0.2196078431, 0.6470588235294118, 0.7843137255, 0.2274509804, 0.2117647059, 0.6509803921568628, 0.7921568627, 0.2352941176, 0.2039215686, 0.6549019607843137, 0.8, 0.2392156863, 0.1960784314, 0.6588235294117647, 0.8078431373, 0.2470588235, 0.1882352941, 0.6627450980392157, 0.8156862745, 0.2509803922, 0.1803921569, 0.6666666666666666, 0.8235294118, 0.2549019608, 0.1725490196, 0.6705882352941176, 0.831372549, 0.2588235294, 0.1647058824, 0.6745098039215687, 0.8392156863, 0.2666666667, 0.1568627451, 0.6784313725490196, 0.8470588235, 0.2705882353, 0.1490196078, 0.6823529411764706, 0.8549019608, 0.2784313725, 0.1411764706, 0.6862745098039216, 0.862745098, 0.2823529412, 0.1333333333, 0.6901960784313725, 0.8705882353, 0.2901960784, 0.1254901961, 0.6941176470588235, 0.8784313725, 0.2941176471, 0.1176470588, 0.6980392156862745, 0.8862745098, 0.3019607843, 0.1098039216, 0.7019607843137254, 0.8941176471, 0.3058823529, 0.1019607843, 0.7058823529411765, 0.9019607843, 0.3137254902, 0.0941176471, 0.7098039215686275, 0.9098039216, 0.3176470588, 0.0862745098, 0.7137254901960784, 0.9176470588, 0.3254901961, 0.0784313725, 0.7176470588235294, 0.9254901961, 0.3294117647, 0.0705882353, 0.7215686274509804, 0.9333333333, 0.337254902, 0.062745098, 0.7254901960784313, 0.9411764706, 0.3411764706, 0.0549019608, 0.7294117647058823, 0.9490196078, 0.3490196078, 0.0470588235, 0.7333333333333333, 0.9568627451, 0.3529411765, 0.0392156863, 0.7372549019607844, 0.9647058824, 0.3607843137, 0.031372549, 0.7411764705882353, 0.9725490196, 0.3647058824, 0.0235294118, 0.7450980392156863, 0.9803921569, 0.3725490196, 0.0156862745, 0.7490196078431373, 0.9882352941, 0.3725490196, 0.0039215686, 0.7529411764705882, 0.9960784314, 0.3843137255, 0.0156862745, 0.7568627450980392, 0.9960784314, 0.3921568627, 0.031372549, 0.7607843137254902, 0.9960784314, 0.4039215686, 0.0470588235, 0.7647058823529411, 0.9960784314, 0.4117647059, 0.062745098, 0.7686274509803922, 0.9960784314, 0.4235294118, 0.0784313725, 0.7725490196078432, 0.9960784314, 0.431372549, 0.0941176471, 0.7764705882352941, 0.9960784314, 0.4431372549, 0.1098039216, 0.7803921568627451, 0.9960784314, 0.4509803922, 0.1254901961, 0.7843137254901961, 0.9960784314, 0.462745098, 0.1411764706, 0.788235294117647, 0.9960784314, 0.4705882353, 0.1568627451, 0.792156862745098, 0.9960784314, 0.4823529412, 0.1725490196, 0.796078431372549, 0.9960784314, 0.4901960784, 0.1882352941, 0.8, 0.9960784314, 0.5019607843, 0.2039215686, 0.803921568627451, 0.9960784314, 0.5098039216, 0.2196078431, 0.807843137254902, 0.9960784314, 0.5215686275, 0.2352941176, 0.8117647058823529, 0.9960784314, 0.5294117647, 0.2509803922, 0.8156862745098039, 0.9960784314, 0.5411764706, 0.262745098, 0.8196078431372549, 0.9960784314, 0.5490196078, 0.2784313725, 0.8235294117647058, 0.9960784314, 0.5607843137, 0.2941176471, 0.8274509803921568, 0.9960784314, 0.568627451, 0.3098039216, 0.8313725490196079, 0.9960784314, 0.5803921569, 0.3254901961, 0.8352941176470589, 0.9960784314, 0.5882352941, 0.3411764706, 0.8392156862745098, 0.9960784314, 0.6, 0.3568627451, 0.8431372549019608, 0.9960784314, 0.6078431373, 0.3725490196, 0.8470588235294118, 0.9960784314, 0.6196078431, 0.3882352941, 0.8509803921568627, 0.9960784314, 0.6274509804, 0.4039215686, 0.8549019607843137, 0.9960784314, 0.6392156863, 0.4196078431, 0.8588235294117647, 0.9960784314, 0.6470588235, 0.4352941176, 0.8627450980392157, 0.9960784314, 0.6588235294, 0.4509803922, 0.8666666666666667, 0.9960784314, 0.6666666667, 0.4666666667, 0.8705882352941177, 0.9960784314, 0.6784313725, 0.4823529412, 0.8745098039215686, 0.9960784314, 0.6862745098, 0.4980392157, 0.8784313725490196, 0.9960784314, 0.6980392157, 0.5137254902, 0.8823529411764706, 0.9960784314, 0.7058823529, 0.5294117647, 0.8862745098039215, 0.9960784314, 0.7176470588, 0.5450980392, 0.8901960784313725, 0.9960784314, 0.7254901961, 0.5607843137, 0.8941176470588236, 0.9960784314, 0.737254902, 0.5764705882, 0.8980392156862745, 0.9960784314, 0.7450980392, 0.5921568627, 0.9019607843137255, 0.9960784314, 0.7529411765, 0.6078431373, 0.9058823529411765, 0.9960784314, 0.7607843137, 0.6235294118, 0.9098039215686274, 0.9960784314, 0.7725490196, 0.6392156863, 0.9137254901960784, 0.9960784314, 0.7803921569, 0.6549019608, 0.9176470588235294, 0.9960784314, 0.7921568627, 0.6705882353, 0.9215686274509803, 0.9960784314, 0.8, 0.6862745098, 0.9254901960784314, 0.9960784314, 0.8117647059, 0.7019607843, 0.9294117647058824, 0.9960784314, 0.8196078431, 0.7176470588, 0.9333333333333333, 0.9960784314, 0.831372549, 0.7333333333, 0.9372549019607843, 0.9960784314, 0.8392156863, 0.7490196078, 0.9411764705882354, 0.9960784314, 0.8509803922, 0.7607843137, 0.9450980392156864, 0.9960784314, 0.8588235294, 0.7764705882, 0.9490196078431372, 0.9960784314, 0.8705882353, 0.7921568627, 0.9529411764705882, 0.9960784314, 0.8784313725, 0.8078431373, 0.9568627450980394, 0.9960784314, 0.8901960784, 0.8235294118, 0.9607843137254903, 0.9960784314, 0.8980392157, 0.8392156863, 0.9647058823529413, 0.9960784314, 0.9098039216, 0.8549019608, 0.9686274509803922, 0.9960784314, 0.9176470588, 0.8705882353, 0.9725490196078431, 0.9960784314, 0.9294117647, 0.8862745098, 0.9764705882352941, 0.9960784314, 0.937254902, 0.9019607843, 0.9803921568627451, 0.9960784314, 0.9490196078, 0.9176470588, 0.984313725490196, 0.9960784314, 0.9568627451, 0.9333333333, 0.9882352941176471, 0.9960784314, 0.968627451, 0.9490196078, 0.9921568627450981, 0.9960784314, 0.9764705882, 0.9647058824, 0.996078431372549, 0.9960784314, 0.9882352941, 0.9803921569, 1.0, 0.9960784314, 0.9882352941, 0.9803921569]
}, {
  ColorSpace: 'RGB',
  Name: 'ge',
  RGBPoints: [0.0, 0.0078431373, 0.0078431373, 0.0078431373, 0.00392156862745098, 0.0078431373, 0.0078431373, 0.0078431373, 0.00784313725490196, 0.0078431373, 0.0078431373, 0.0078431373, 0.011764705882352941, 0.0078431373, 0.0078431373, 0.0078431373, 0.01568627450980392, 0.0078431373, 0.0078431373, 0.0078431373, 0.0196078431372549, 0.0078431373, 0.0078431373, 0.0078431373, 0.023529411764705882, 0.0078431373, 0.0078431373, 0.0078431373, 0.027450980392156862, 0.0078431373, 0.0078431373, 0.0078431373, 0.03137254901960784, 0.0078431373, 0.0078431373, 0.0078431373, 0.03529411764705882, 0.0078431373, 0.0078431373, 0.0078431373, 0.0392156862745098, 0.0078431373, 0.0078431373, 0.0078431373, 0.043137254901960784, 0.0078431373, 0.0078431373, 0.0078431373, 0.047058823529411764, 0.0078431373, 0.0078431373, 0.0078431373, 0.050980392156862744, 0.0078431373, 0.0078431373, 0.0078431373, 0.054901960784313725, 0.0078431373, 0.0078431373, 0.0078431373, 0.05882352941176471, 0.0117647059, 0.0078431373, 0.0078431373, 0.06274509803921569, 0.0078431373, 0.0156862745, 0.0156862745, 0.06666666666666667, 0.0078431373, 0.0235294118, 0.0235294118, 0.07058823529411765, 0.0078431373, 0.031372549, 0.031372549, 0.07450980392156863, 0.0078431373, 0.0392156863, 0.0392156863, 0.0784313725490196, 0.0078431373, 0.0470588235, 0.0470588235, 0.08235294117647059, 0.0078431373, 0.0549019608, 0.0549019608, 0.08627450980392157, 0.0078431373, 0.062745098, 0.062745098, 0.09019607843137255, 0.0078431373, 0.0705882353, 0.0705882353, 0.09411764705882353, 0.0078431373, 0.0784313725, 0.0784313725, 0.09803921568627451, 0.0078431373, 0.0901960784, 0.0862745098, 0.10196078431372549, 0.0078431373, 0.0980392157, 0.0941176471, 0.10588235294117647, 0.0078431373, 0.1058823529, 0.1019607843, 0.10980392156862745, 0.0078431373, 0.1137254902, 0.1098039216, 0.11372549019607843, 0.0078431373, 0.1215686275, 0.1176470588, 0.11764705882352942, 0.0078431373, 0.1294117647, 0.1254901961, 0.12156862745098039, 0.0078431373, 0.137254902, 0.1333333333, 0.12549019607843137, 0.0078431373, 0.1450980392, 0.1411764706, 0.12941176470588237, 0.0078431373, 0.1529411765, 0.1490196078, 0.13333333333333333, 0.0078431373, 0.1647058824, 0.1568627451, 0.13725490196078433, 0.0078431373, 0.1725490196, 0.1647058824, 0.1411764705882353, 0.0078431373, 0.1803921569, 0.1725490196, 0.1450980392156863, 0.0078431373, 0.1882352941, 0.1803921569, 0.14901960784313725, 0.0078431373, 0.1960784314, 0.1882352941, 0.15294117647058825, 0.0078431373, 0.2039215686, 0.1960784314, 0.1568627450980392, 0.0078431373, 0.2117647059, 0.2039215686, 0.1607843137254902, 0.0078431373, 0.2196078431, 0.2117647059, 0.16470588235294117, 0.0078431373, 0.2274509804, 0.2196078431, 0.16862745098039217, 0.0078431373, 0.2352941176, 0.2274509804, 0.17254901960784313, 0.0078431373, 0.2470588235, 0.2352941176, 0.17647058823529413, 0.0078431373, 0.2509803922, 0.2431372549, 0.1803921568627451, 0.0078431373, 0.2549019608, 0.2509803922, 0.1843137254901961, 0.0078431373, 0.262745098, 0.2509803922, 0.18823529411764706, 0.0078431373, 0.2705882353, 0.2588235294, 0.19215686274509805, 0.0078431373, 0.2784313725, 0.2666666667, 0.19607843137254902, 0.0078431373, 0.2862745098, 0.2745098039, 0.2, 0.0078431373, 0.2941176471, 0.2823529412, 0.20392156862745098, 0.0078431373, 0.3019607843, 0.2901960784, 0.20784313725490197, 0.0078431373, 0.3137254902, 0.2980392157, 0.21176470588235294, 0.0078431373, 0.3215686275, 0.3058823529, 0.21568627450980393, 0.0078431373, 0.3294117647, 0.3137254902, 0.2196078431372549, 0.0078431373, 0.337254902, 0.3215686275, 0.2235294117647059, 0.0078431373, 0.3450980392, 0.3294117647, 0.22745098039215686, 0.0078431373, 0.3529411765, 0.337254902, 0.23137254901960785, 0.0078431373, 0.3607843137, 0.3450980392, 0.23529411764705885, 0.0078431373, 0.368627451, 0.3529411765, 0.23921568627450984, 0.0078431373, 0.3764705882, 0.3607843137, 0.24313725490196078, 0.0078431373, 0.3843137255, 0.368627451, 0.24705882352941178, 0.0078431373, 0.3960784314, 0.3764705882, 0.25098039215686274, 0.0078431373, 0.4039215686, 0.3843137255, 0.2549019607843137, 0.0078431373, 0.4117647059, 0.3921568627, 0.25882352941176473, 0.0078431373, 0.4196078431, 0.4, 0.2627450980392157, 0.0078431373, 0.4274509804, 0.4078431373, 0.26666666666666666, 0.0078431373, 0.4352941176, 0.4156862745, 0.27058823529411763, 0.0078431373, 0.4431372549, 0.4235294118, 0.27450980392156865, 0.0078431373, 0.4509803922, 0.431372549, 0.2784313725490196, 0.0078431373, 0.4588235294, 0.4392156863, 0.2823529411764706, 0.0078431373, 0.4705882353, 0.4470588235, 0.28627450980392155, 0.0078431373, 0.4784313725, 0.4549019608, 0.2901960784313726, 0.0078431373, 0.4862745098, 0.462745098, 0.29411764705882354, 0.0078431373, 0.4941176471, 0.4705882353, 0.2980392156862745, 0.0078431373, 0.5019607843, 0.4784313725, 0.30196078431372547, 0.0117647059, 0.5098039216, 0.4862745098, 0.3058823529411765, 0.0196078431, 0.5019607843, 0.4941176471, 0.30980392156862746, 0.0274509804, 0.4941176471, 0.5058823529, 0.3137254901960784, 0.0352941176, 0.4862745098, 0.5137254902, 0.3176470588235294, 0.0431372549, 0.4784313725, 0.5215686275, 0.3215686274509804, 0.0509803922, 0.4705882353, 0.5294117647, 0.3254901960784314, 0.0588235294, 0.462745098, 0.537254902, 0.32941176470588235, 0.0666666667, 0.4549019608, 0.5450980392, 0.3333333333333333, 0.0745098039, 0.4470588235, 0.5529411765, 0.33725490196078434, 0.0823529412, 0.4392156863, 0.5607843137, 0.3411764705882353, 0.0901960784, 0.431372549, 0.568627451, 0.34509803921568627, 0.0980392157, 0.4235294118, 0.5764705882, 0.34901960784313724, 0.1058823529, 0.4156862745, 0.5843137255, 0.35294117647058826, 0.1137254902, 0.4078431373, 0.5921568627, 0.3568627450980392, 0.1215686275, 0.4, 0.6, 0.3607843137254902, 0.1294117647, 0.3921568627, 0.6078431373, 0.36470588235294116, 0.137254902, 0.3843137255, 0.6156862745, 0.3686274509803922, 0.1450980392, 0.3764705882, 0.6235294118, 0.37254901960784315, 0.1529411765, 0.368627451, 0.631372549, 0.3764705882352941, 0.1607843137, 0.3607843137, 0.6392156863, 0.3803921568627451, 0.168627451, 0.3529411765, 0.6470588235, 0.3843137254901961, 0.1764705882, 0.3450980392, 0.6549019608, 0.38823529411764707, 0.1843137255, 0.337254902, 0.662745098, 0.39215686274509803, 0.1921568627, 0.3294117647, 0.6705882353, 0.396078431372549, 0.2, 0.3215686275, 0.6784313725, 0.4, 0.2078431373, 0.3137254902, 0.6862745098, 0.403921568627451, 0.2156862745, 0.3058823529, 0.6941176471, 0.40784313725490196, 0.2235294118, 0.2980392157, 0.7019607843, 0.4117647058823529, 0.231372549, 0.2901960784, 0.7098039216, 0.41568627450980394, 0.2392156863, 0.2823529412, 0.7176470588, 0.4196078431372549, 0.2470588235, 0.2745098039, 0.7254901961, 0.4235294117647059, 0.2509803922, 0.2666666667, 0.7333333333, 0.42745098039215684, 0.2509803922, 0.2588235294, 0.7411764706, 0.43137254901960786, 0.2588235294, 0.2509803922, 0.7490196078, 0.43529411764705883, 0.2666666667, 0.2509803922, 0.7490196078, 0.4392156862745098, 0.2745098039, 0.2431372549, 0.7568627451, 0.44313725490196076, 0.2823529412, 0.2352941176, 0.7647058824, 0.4470588235294118, 0.2901960784, 0.2274509804, 0.7725490196, 0.45098039215686275, 0.2980392157, 0.2196078431, 0.7803921569, 0.4549019607843137, 0.3058823529, 0.2117647059, 0.7882352941, 0.4588235294117647, 0.3137254902, 0.2039215686, 0.7960784314, 0.4627450980392157, 0.3215686275, 0.1960784314, 0.8039215686, 0.4666666666666667, 0.3294117647, 0.1882352941, 0.8117647059, 0.4705882352941177, 0.337254902, 0.1803921569, 0.8196078431, 0.4745098039215686, 0.3450980392, 0.1725490196, 0.8274509804, 0.4784313725490197, 0.3529411765, 0.1647058824, 0.8352941176, 0.48235294117647065, 0.3607843137, 0.1568627451, 0.8431372549, 0.48627450980392156, 0.368627451, 0.1490196078, 0.8509803922, 0.49019607843137253, 0.3764705882, 0.1411764706, 0.8588235294, 0.49411764705882355, 0.3843137255, 0.1333333333, 0.8666666667, 0.4980392156862745, 0.3921568627, 0.1254901961, 0.8745098039, 0.5019607843137255, 0.4, 0.1176470588, 0.8823529412, 0.5058823529411764, 0.4078431373, 0.1098039216, 0.8901960784, 0.5098039215686274, 0.4156862745, 0.1019607843, 0.8980392157, 0.5137254901960784, 0.4235294118, 0.0941176471, 0.9058823529, 0.5176470588235295, 0.431372549, 0.0862745098, 0.9137254902, 0.5215686274509804, 0.4392156863, 0.0784313725, 0.9215686275, 0.5254901960784314, 0.4470588235, 0.0705882353, 0.9294117647, 0.5294117647058824, 0.4549019608, 0.062745098, 0.937254902, 0.5333333333333333, 0.462745098, 0.0549019608, 0.9450980392, 0.5372549019607843, 0.4705882353, 0.0470588235, 0.9529411765, 0.5411764705882353, 0.4784313725, 0.0392156863, 0.9607843137, 0.5450980392156862, 0.4862745098, 0.031372549, 0.968627451, 0.5490196078431373, 0.4941176471, 0.0235294118, 0.9764705882, 0.5529411764705883, 0.4980392157, 0.0156862745, 0.9843137255, 0.5568627450980392, 0.5058823529, 0.0078431373, 0.9921568627, 0.5607843137254902, 0.5137254902, 0.0156862745, 0.9803921569, 0.5647058823529412, 0.5215686275, 0.0235294118, 0.9647058824, 0.5686274509803921, 0.5294117647, 0.0352941176, 0.9490196078, 0.5725490196078431, 0.537254902, 0.0431372549, 0.9333333333, 0.5764705882352941, 0.5450980392, 0.0509803922, 0.9176470588, 0.5803921568627451, 0.5529411765, 0.062745098, 0.9019607843, 0.5843137254901961, 0.5607843137, 0.0705882353, 0.8862745098, 0.5882352941176471, 0.568627451, 0.0784313725, 0.8705882353, 0.592156862745098, 0.5764705882, 0.0901960784, 0.8549019608, 0.596078431372549, 0.5843137255, 0.0980392157, 0.8392156863, 0.6, 0.5921568627, 0.1098039216, 0.8235294118, 0.6039215686274509, 0.6, 0.1176470588, 0.8078431373, 0.6078431372549019, 0.6078431373, 0.1254901961, 0.7921568627, 0.611764705882353, 0.6156862745, 0.137254902, 0.7764705882, 0.615686274509804, 0.6235294118, 0.1450980392, 0.7607843137, 0.6196078431372549, 0.631372549, 0.1529411765, 0.7490196078, 0.6235294117647059, 0.6392156863, 0.1647058824, 0.737254902, 0.6274509803921569, 0.6470588235, 0.1725490196, 0.7215686275, 0.6313725490196078, 0.6549019608, 0.1843137255, 0.7058823529, 0.6352941176470588, 0.662745098, 0.1921568627, 0.6901960784, 0.6392156862745098, 0.6705882353, 0.2, 0.6745098039, 0.6431372549019608, 0.6784313725, 0.2117647059, 0.6588235294, 0.6470588235294118, 0.6862745098, 0.2196078431, 0.6431372549, 0.6509803921568628, 0.6941176471, 0.2274509804, 0.6274509804, 0.6549019607843137, 0.7019607843, 0.2392156863, 0.6117647059, 0.6588235294117647, 0.7098039216, 0.2470588235, 0.5960784314, 0.6627450980392157, 0.7176470588, 0.2509803922, 0.5803921569, 0.6666666666666666, 0.7254901961, 0.2588235294, 0.5647058824, 0.6705882352941176, 0.7333333333, 0.2666666667, 0.5490196078, 0.6745098039215687, 0.7411764706, 0.2784313725, 0.5333333333, 0.6784313725490196, 0.7490196078, 0.2862745098, 0.5176470588, 0.6823529411764706, 0.7490196078, 0.2941176471, 0.5019607843, 0.6862745098039216, 0.7529411765, 0.3058823529, 0.4862745098, 0.6901960784313725, 0.7607843137, 0.3137254902, 0.4705882353, 0.6941176470588235, 0.768627451, 0.3215686275, 0.4549019608, 0.6980392156862745, 0.7764705882, 0.3333333333, 0.4392156863, 0.7019607843137254, 0.7843137255, 0.3411764706, 0.4235294118, 0.7058823529411765, 0.7921568627, 0.3529411765, 0.4078431373, 0.7098039215686275, 0.8, 0.3607843137, 0.3921568627, 0.7137254901960784, 0.8078431373, 0.368627451, 0.3764705882, 0.7176470588235294, 0.8156862745, 0.3803921569, 0.3607843137, 0.7215686274509804, 0.8235294118, 0.3882352941, 0.3450980392, 0.7254901960784313, 0.831372549, 0.3960784314, 0.3294117647, 0.7294117647058823, 0.8392156863, 0.4078431373, 0.3137254902, 0.7333333333333333, 0.8470588235, 0.4156862745, 0.2980392157, 0.7372549019607844, 0.8549019608, 0.4274509804, 0.2823529412, 0.7411764705882353, 0.862745098, 0.4352941176, 0.2666666667, 0.7450980392156863, 0.8705882353, 0.4431372549, 0.2509803922, 0.7490196078431373, 0.8784313725, 0.4549019608, 0.2431372549, 0.7529411764705882, 0.8862745098, 0.462745098, 0.2274509804, 0.7568627450980392, 0.8941176471, 0.4705882353, 0.2117647059, 0.7607843137254902, 0.9019607843, 0.4823529412, 0.1960784314, 0.7647058823529411, 0.9098039216, 0.4901960784, 0.1803921569, 0.7686274509803922, 0.9176470588, 0.4980392157, 0.1647058824, 0.7725490196078432, 0.9254901961, 0.5098039216, 0.1490196078, 0.7764705882352941, 0.9333333333, 0.5176470588, 0.1333333333, 0.7803921568627451, 0.9411764706, 0.5294117647, 0.1176470588, 0.7843137254901961, 0.9490196078, 0.537254902, 0.1019607843, 0.788235294117647, 0.9568627451, 0.5450980392, 0.0862745098, 0.792156862745098, 0.9647058824, 0.5568627451, 0.0705882353, 0.796078431372549, 0.9725490196, 0.5647058824, 0.0549019608, 0.8, 0.9803921569, 0.5725490196, 0.0392156863, 0.803921568627451, 0.9882352941, 0.5843137255, 0.0235294118, 0.807843137254902, 0.9921568627, 0.5921568627, 0.0078431373, 0.8117647058823529, 0.9921568627, 0.6039215686, 0.0274509804, 0.8156862745098039, 0.9921568627, 0.6117647059, 0.0509803922, 0.8196078431372549, 0.9921568627, 0.6196078431, 0.0745098039, 0.8235294117647058, 0.9921568627, 0.631372549, 0.0980392157, 0.8274509803921568, 0.9921568627, 0.6392156863, 0.1215686275, 0.8313725490196079, 0.9921568627, 0.6470588235, 0.1411764706, 0.8352941176470589, 0.9921568627, 0.6588235294, 0.1647058824, 0.8392156862745098, 0.9921568627, 0.6666666667, 0.1882352941, 0.8431372549019608, 0.9921568627, 0.6784313725, 0.2117647059, 0.8470588235294118, 0.9921568627, 0.6862745098, 0.2352941176, 0.8509803921568627, 0.9921568627, 0.6941176471, 0.2509803922, 0.8549019607843137, 0.9921568627, 0.7058823529, 0.2705882353, 0.8588235294117647, 0.9921568627, 0.7137254902, 0.2941176471, 0.8627450980392157, 0.9921568627, 0.7215686275, 0.3176470588, 0.8666666666666667, 0.9921568627, 0.7333333333, 0.3411764706, 0.8705882352941177, 0.9921568627, 0.7411764706, 0.3647058824, 0.8745098039215686, 0.9921568627, 0.7490196078, 0.3843137255, 0.8784313725490196, 0.9921568627, 0.7529411765, 0.4078431373, 0.8823529411764706, 0.9921568627, 0.7607843137, 0.431372549, 0.8862745098039215, 0.9921568627, 0.7725490196, 0.4549019608, 0.8901960784313725, 0.9921568627, 0.7803921569, 0.4784313725, 0.8941176470588236, 0.9921568627, 0.7882352941, 0.4980392157, 0.8980392156862745, 0.9921568627, 0.8, 0.5215686275, 0.9019607843137255, 0.9921568627, 0.8078431373, 0.5450980392, 0.9058823529411765, 0.9921568627, 0.8156862745, 0.568627451, 0.9098039215686274, 0.9921568627, 0.8274509804, 0.5921568627, 0.9137254901960784, 0.9921568627, 0.8352941176, 0.6156862745, 0.9176470588235294, 0.9921568627, 0.8470588235, 0.6352941176, 0.9215686274509803, 0.9921568627, 0.8549019608, 0.6588235294, 0.9254901960784314, 0.9921568627, 0.862745098, 0.6823529412, 0.9294117647058824, 0.9921568627, 0.8745098039, 0.7058823529, 0.9333333333333333, 0.9921568627, 0.8823529412, 0.7294117647, 0.9372549019607843, 0.9921568627, 0.8901960784, 0.7490196078, 0.9411764705882354, 0.9921568627, 0.9019607843, 0.7647058824, 0.9450980392156864, 0.9921568627, 0.9098039216, 0.7882352941, 0.9490196078431372, 0.9921568627, 0.9215686275, 0.8117647059, 0.9529411764705882, 0.9921568627, 0.9294117647, 0.8352941176, 0.9568627450980394, 0.9921568627, 0.937254902, 0.8588235294, 0.9607843137254903, 0.9921568627, 0.9490196078, 0.8784313725, 0.9647058823529413, 0.9921568627, 0.9568627451, 0.9019607843, 0.9686274509803922, 0.9921568627, 0.9647058824, 0.9254901961, 0.9725490196078431, 0.9921568627, 0.9764705882, 0.9490196078, 0.9764705882352941, 0.9921568627, 0.9843137255, 0.9725490196, 0.9803921568627451, 0.9921568627, 0.9921568627, 0.9921568627, 0.984313725490196, 0.9921568627, 0.9921568627, 0.9921568627, 0.9882352941176471, 0.9921568627, 0.9921568627, 0.9921568627, 0.9921568627450981, 0.9921568627, 0.9921568627, 0.9921568627, 0.996078431372549, 0.9921568627, 0.9921568627, 0.9921568627, 1.0, 0.9921568627, 0.9921568627, 0.9921568627]
}, {
  ColorSpace: 'RGB',
  Name: 'siemens',
  RGBPoints: [0.0, 0.0078431373, 0.0039215686, 0.1254901961, 0.00392156862745098, 0.0078431373, 0.0039215686, 0.1254901961, 0.00784313725490196, 0.0078431373, 0.0039215686, 0.1882352941, 0.011764705882352941, 0.0117647059, 0.0039215686, 0.2509803922, 0.01568627450980392, 0.0117647059, 0.0039215686, 0.3098039216, 0.0196078431372549, 0.0156862745, 0.0039215686, 0.3725490196, 0.023529411764705882, 0.0156862745, 0.0039215686, 0.3725490196, 0.027450980392156862, 0.0156862745, 0.0039215686, 0.3725490196, 0.03137254901960784, 0.0156862745, 0.0039215686, 0.3725490196, 0.03529411764705882, 0.0156862745, 0.0039215686, 0.3725490196, 0.0392156862745098, 0.0156862745, 0.0039215686, 0.3725490196, 0.043137254901960784, 0.0156862745, 0.0039215686, 0.3725490196, 0.047058823529411764, 0.0156862745, 0.0039215686, 0.3725490196, 0.050980392156862744, 0.0156862745, 0.0039215686, 0.3725490196, 0.054901960784313725, 0.0156862745, 0.0039215686, 0.3725490196, 0.05882352941176471, 0.0156862745, 0.0039215686, 0.3725490196, 0.06274509803921569, 0.0156862745, 0.0039215686, 0.3882352941, 0.06666666666666667, 0.0156862745, 0.0039215686, 0.4078431373, 0.07058823529411765, 0.0156862745, 0.0039215686, 0.4235294118, 0.07450980392156863, 0.0156862745, 0.0039215686, 0.4431372549, 0.0784313725490196, 0.0156862745, 0.0039215686, 0.462745098, 0.08235294117647059, 0.0156862745, 0.0039215686, 0.4784313725, 0.08627450980392157, 0.0156862745, 0.0039215686, 0.4980392157, 0.09019607843137255, 0.0196078431, 0.0039215686, 0.5137254902, 0.09411764705882353, 0.0196078431, 0.0039215686, 0.5333333333, 0.09803921568627451, 0.0196078431, 0.0039215686, 0.5529411765, 0.10196078431372549, 0.0196078431, 0.0039215686, 0.568627451, 0.10588235294117647, 0.0196078431, 0.0039215686, 0.5882352941, 0.10980392156862745, 0.0196078431, 0.0039215686, 0.6039215686, 0.11372549019607843, 0.0196078431, 0.0039215686, 0.6235294118, 0.11764705882352942, 0.0196078431, 0.0039215686, 0.6431372549, 0.12156862745098039, 0.0235294118, 0.0039215686, 0.6588235294, 0.12549019607843137, 0.0235294118, 0.0039215686, 0.6784313725, 0.12941176470588237, 0.0235294118, 0.0039215686, 0.6980392157, 0.13333333333333333, 0.0235294118, 0.0039215686, 0.7137254902, 0.13725490196078433, 0.0235294118, 0.0039215686, 0.7333333333, 0.1411764705882353, 0.0235294118, 0.0039215686, 0.7490196078, 0.1450980392156863, 0.0235294118, 0.0039215686, 0.7647058824, 0.14901960784313725, 0.0235294118, 0.0039215686, 0.7843137255, 0.15294117647058825, 0.0274509804, 0.0039215686, 0.8, 0.1568627450980392, 0.0274509804, 0.0039215686, 0.8196078431, 0.1607843137254902, 0.0274509804, 0.0039215686, 0.8352941176, 0.16470588235294117, 0.0274509804, 0.0039215686, 0.8549019608, 0.16862745098039217, 0.0274509804, 0.0039215686, 0.8745098039, 0.17254901960784313, 0.0274509804, 0.0039215686, 0.8901960784, 0.17647058823529413, 0.0274509804, 0.0039215686, 0.9098039216, 0.1803921568627451, 0.031372549, 0.0039215686, 0.9294117647, 0.1843137254901961, 0.031372549, 0.0039215686, 0.9254901961, 0.18823529411764706, 0.0509803922, 0.0039215686, 0.9098039216, 0.19215686274509805, 0.0705882353, 0.0039215686, 0.8901960784, 0.19607843137254902, 0.0901960784, 0.0039215686, 0.8705882353, 0.2, 0.1137254902, 0.0039215686, 0.8509803922, 0.20392156862745098, 0.1333333333, 0.0039215686, 0.831372549, 0.20784313725490197, 0.1529411765, 0.0039215686, 0.8117647059, 0.21176470588235294, 0.1725490196, 0.0039215686, 0.7921568627, 0.21568627450980393, 0.1960784314, 0.0039215686, 0.7725490196, 0.2196078431372549, 0.2156862745, 0.0039215686, 0.7529411765, 0.2235294117647059, 0.2352941176, 0.0039215686, 0.737254902, 0.22745098039215686, 0.2509803922, 0.0039215686, 0.7176470588, 0.23137254901960785, 0.2745098039, 0.0039215686, 0.6980392157, 0.23529411764705885, 0.2941176471, 0.0039215686, 0.6784313725, 0.23921568627450984, 0.3137254902, 0.0039215686, 0.6588235294, 0.24313725490196078, 0.3333333333, 0.0039215686, 0.6392156863, 0.24705882352941178, 0.3568627451, 0.0039215686, 0.6196078431, 0.25098039215686274, 0.3764705882, 0.0039215686, 0.6, 0.2549019607843137, 0.3960784314, 0.0039215686, 0.5803921569, 0.25882352941176473, 0.4156862745, 0.0039215686, 0.5607843137, 0.2627450980392157, 0.4392156863, 0.0039215686, 0.5411764706, 0.26666666666666666, 0.4588235294, 0.0039215686, 0.5215686275, 0.27058823529411763, 0.4784313725, 0.0039215686, 0.5019607843, 0.27450980392156865, 0.4980392157, 0.0039215686, 0.4823529412, 0.2784313725490196, 0.5215686275, 0.0039215686, 0.4666666667, 0.2823529411764706, 0.5411764706, 0.0039215686, 0.4470588235, 0.28627450980392155, 0.5607843137, 0.0039215686, 0.4274509804, 0.2901960784313726, 0.5803921569, 0.0039215686, 0.4078431373, 0.29411764705882354, 0.6039215686, 0.0039215686, 0.3882352941, 0.2980392156862745, 0.6235294118, 0.0039215686, 0.368627451, 0.30196078431372547, 0.6431372549, 0.0039215686, 0.3490196078, 0.3058823529411765, 0.662745098, 0.0039215686, 0.3294117647, 0.30980392156862746, 0.6862745098, 0.0039215686, 0.3098039216, 0.3137254901960784, 0.7058823529, 0.0039215686, 0.2901960784, 0.3176470588235294, 0.7254901961, 0.0039215686, 0.2705882353, 0.3215686274509804, 0.7450980392, 0.0039215686, 0.2509803922, 0.3254901960784314, 0.7647058824, 0.0039215686, 0.2352941176, 0.32941176470588235, 0.7843137255, 0.0039215686, 0.2156862745, 0.3333333333333333, 0.8039215686, 0.0039215686, 0.1960784314, 0.33725490196078434, 0.8235294118, 0.0039215686, 0.1764705882, 0.3411764705882353, 0.8470588235, 0.0039215686, 0.1568627451, 0.34509803921568627, 0.8666666667, 0.0039215686, 0.137254902, 0.34901960784313724, 0.8862745098, 0.0039215686, 0.1176470588, 0.35294117647058826, 0.9058823529, 0.0039215686, 0.0980392157, 0.3568627450980392, 0.9294117647, 0.0039215686, 0.0784313725, 0.3607843137254902, 0.9490196078, 0.0039215686, 0.0588235294, 0.36470588235294116, 0.968627451, 0.0039215686, 0.0392156863, 0.3686274509803922, 0.9921568627, 0.0039215686, 0.0235294118, 0.37254901960784315, 0.9529411765, 0.0039215686, 0.0588235294, 0.3764705882352941, 0.9529411765, 0.0078431373, 0.0549019608, 0.3803921568627451, 0.9529411765, 0.0156862745, 0.0549019608, 0.3843137254901961, 0.9529411765, 0.0235294118, 0.0549019608, 0.38823529411764707, 0.9529411765, 0.031372549, 0.0549019608, 0.39215686274509803, 0.9529411765, 0.0352941176, 0.0549019608, 0.396078431372549, 0.9529411765, 0.0431372549, 0.0549019608, 0.4, 0.9529411765, 0.0509803922, 0.0549019608, 0.403921568627451, 0.9529411765, 0.0588235294, 0.0549019608, 0.40784313725490196, 0.9529411765, 0.062745098, 0.0549019608, 0.4117647058823529, 0.9529411765, 0.0705882353, 0.0549019608, 0.41568627450980394, 0.9529411765, 0.0784313725, 0.0509803922, 0.4196078431372549, 0.9529411765, 0.0862745098, 0.0509803922, 0.4235294117647059, 0.9568627451, 0.0941176471, 0.0509803922, 0.42745098039215684, 0.9568627451, 0.0980392157, 0.0509803922, 0.43137254901960786, 0.9568627451, 0.1058823529, 0.0509803922, 0.43529411764705883, 0.9568627451, 0.1137254902, 0.0509803922, 0.4392156862745098, 0.9568627451, 0.1215686275, 0.0509803922, 0.44313725490196076, 0.9568627451, 0.1254901961, 0.0509803922, 0.4470588235294118, 0.9568627451, 0.1333333333, 0.0509803922, 0.45098039215686275, 0.9568627451, 0.1411764706, 0.0509803922, 0.4549019607843137, 0.9568627451, 0.1490196078, 0.0470588235, 0.4588235294117647, 0.9568627451, 0.1568627451, 0.0470588235, 0.4627450980392157, 0.9568627451, 0.1607843137, 0.0470588235, 0.4666666666666667, 0.9568627451, 0.168627451, 0.0470588235, 0.4705882352941177, 0.9607843137, 0.1764705882, 0.0470588235, 0.4745098039215686, 0.9607843137, 0.1843137255, 0.0470588235, 0.4784313725490197, 0.9607843137, 0.1882352941, 0.0470588235, 0.48235294117647065, 0.9607843137, 0.1960784314, 0.0470588235, 0.48627450980392156, 0.9607843137, 0.2039215686, 0.0470588235, 0.49019607843137253, 0.9607843137, 0.2117647059, 0.0470588235, 0.49411764705882355, 0.9607843137, 0.2196078431, 0.0431372549, 0.4980392156862745, 0.9607843137, 0.2235294118, 0.0431372549, 0.5019607843137255, 0.9607843137, 0.231372549, 0.0431372549, 0.5058823529411764, 0.9607843137, 0.2392156863, 0.0431372549, 0.5098039215686274, 0.9607843137, 0.2470588235, 0.0431372549, 0.5137254901960784, 0.9607843137, 0.2509803922, 0.0431372549, 0.5176470588235295, 0.9647058824, 0.2549019608, 0.0431372549, 0.5215686274509804, 0.9647058824, 0.262745098, 0.0431372549, 0.5254901960784314, 0.9647058824, 0.2705882353, 0.0431372549, 0.5294117647058824, 0.9647058824, 0.2745098039, 0.0431372549, 0.5333333333333333, 0.9647058824, 0.2823529412, 0.0392156863, 0.5372549019607843, 0.9647058824, 0.2901960784, 0.0392156863, 0.5411764705882353, 0.9647058824, 0.2980392157, 0.0392156863, 0.5450980392156862, 0.9647058824, 0.3058823529, 0.0392156863, 0.5490196078431373, 0.9647058824, 0.3098039216, 0.0392156863, 0.5529411764705883, 0.9647058824, 0.3176470588, 0.0392156863, 0.5568627450980392, 0.9647058824, 0.3254901961, 0.0392156863, 0.5607843137254902, 0.9647058824, 0.3333333333, 0.0392156863, 0.5647058823529412, 0.9647058824, 0.337254902, 0.0392156863, 0.5686274509803921, 0.968627451, 0.3450980392, 0.0392156863, 0.5725490196078431, 0.968627451, 0.3529411765, 0.0352941176, 0.5764705882352941, 0.968627451, 0.3607843137, 0.0352941176, 0.5803921568627451, 0.968627451, 0.368627451, 0.0352941176, 0.5843137254901961, 0.968627451, 0.3725490196, 0.0352941176, 0.5882352941176471, 0.968627451, 0.3803921569, 0.0352941176, 0.592156862745098, 0.968627451, 0.3882352941, 0.0352941176, 0.596078431372549, 0.968627451, 0.3960784314, 0.0352941176, 0.6, 0.968627451, 0.4, 0.0352941176, 0.6039215686274509, 0.968627451, 0.4078431373, 0.0352941176, 0.6078431372549019, 0.968627451, 0.4156862745, 0.0352941176, 0.611764705882353, 0.968627451, 0.4235294118, 0.031372549, 0.615686274509804, 0.9725490196, 0.431372549, 0.031372549, 0.6196078431372549, 0.9725490196, 0.4352941176, 0.031372549, 0.6235294117647059, 0.9725490196, 0.4431372549, 0.031372549, 0.6274509803921569, 0.9725490196, 0.4509803922, 0.031372549, 0.6313725490196078, 0.9725490196, 0.4588235294, 0.031372549, 0.6352941176470588, 0.9725490196, 0.462745098, 0.031372549, 0.6392156862745098, 0.9725490196, 0.4705882353, 0.031372549, 0.6431372549019608, 0.9725490196, 0.4784313725, 0.031372549, 0.6470588235294118, 0.9725490196, 0.4862745098, 0.031372549, 0.6509803921568628, 0.9725490196, 0.4941176471, 0.0274509804, 0.6549019607843137, 0.9725490196, 0.4980392157, 0.0274509804, 0.6588235294117647, 0.9725490196, 0.5058823529, 0.0274509804, 0.6627450980392157, 0.9764705882, 0.5137254902, 0.0274509804, 0.6666666666666666, 0.9764705882, 0.5215686275, 0.0274509804, 0.6705882352941176, 0.9764705882, 0.5254901961, 0.0274509804, 0.6745098039215687, 0.9764705882, 0.5333333333, 0.0274509804, 0.6784313725490196, 0.9764705882, 0.5411764706, 0.0274509804, 0.6823529411764706, 0.9764705882, 0.5490196078, 0.0274509804, 0.6862745098039216, 0.9764705882, 0.5529411765, 0.0274509804, 0.6901960784313725, 0.9764705882, 0.5607843137, 0.0235294118, 0.6941176470588235, 0.9764705882, 0.568627451, 0.0235294118, 0.6980392156862745, 0.9764705882, 0.5764705882, 0.0235294118, 0.7019607843137254, 0.9764705882, 0.5843137255, 0.0235294118, 0.7058823529411765, 0.9764705882, 0.5882352941, 0.0235294118, 0.7098039215686275, 0.9764705882, 0.5960784314, 0.0235294118, 0.7137254901960784, 0.9803921569, 0.6039215686, 0.0235294118, 0.7176470588235294, 0.9803921569, 0.6117647059, 0.0235294118, 0.7215686274509804, 0.9803921569, 0.6156862745, 0.0235294118, 0.7254901960784313, 0.9803921569, 0.6235294118, 0.0235294118, 0.7294117647058823, 0.9803921569, 0.631372549, 0.0196078431, 0.7333333333333333, 0.9803921569, 0.6392156863, 0.0196078431, 0.7372549019607844, 0.9803921569, 0.6470588235, 0.0196078431, 0.7411764705882353, 0.9803921569, 0.6509803922, 0.0196078431, 0.7450980392156863, 0.9803921569, 0.6588235294, 0.0196078431, 0.7490196078431373, 0.9803921569, 0.6666666667, 0.0196078431, 0.7529411764705882, 0.9803921569, 0.6745098039, 0.0196078431, 0.7568627450980392, 0.9803921569, 0.6784313725, 0.0196078431, 0.7607843137254902, 0.9843137255, 0.6862745098, 0.0196078431, 0.7647058823529411, 0.9843137255, 0.6941176471, 0.0196078431, 0.7686274509803922, 0.9843137255, 0.7019607843, 0.0156862745, 0.7725490196078432, 0.9843137255, 0.7098039216, 0.0156862745, 0.7764705882352941, 0.9843137255, 0.7137254902, 0.0156862745, 0.7803921568627451, 0.9843137255, 0.7215686275, 0.0156862745, 0.7843137254901961, 0.9843137255, 0.7294117647, 0.0156862745, 0.788235294117647, 0.9843137255, 0.737254902, 0.0156862745, 0.792156862745098, 0.9843137255, 0.7411764706, 0.0156862745, 0.796078431372549, 0.9843137255, 0.7490196078, 0.0156862745, 0.8, 0.9843137255, 0.7529411765, 0.0156862745, 0.803921568627451, 0.9843137255, 0.7607843137, 0.0156862745, 0.807843137254902, 0.9882352941, 0.768627451, 0.0156862745, 0.8117647058823529, 0.9882352941, 0.768627451, 0.0156862745, 0.8156862745098039, 0.9843137255, 0.7843137255, 0.0117647059, 0.8196078431372549, 0.9843137255, 0.8, 0.0117647059, 0.8235294117647058, 0.9843137255, 0.8156862745, 0.0117647059, 0.8274509803921568, 0.9803921569, 0.831372549, 0.0117647059, 0.8313725490196079, 0.9803921569, 0.8431372549, 0.0117647059, 0.8352941176470589, 0.9803921569, 0.8588235294, 0.0078431373, 0.8392156862745098, 0.9803921569, 0.8745098039, 0.0078431373, 0.8431372549019608, 0.9764705882, 0.8901960784, 0.0078431373, 0.8470588235294118, 0.9764705882, 0.9058823529, 0.0078431373, 0.8509803921568627, 0.9764705882, 0.9176470588, 0.0078431373, 0.8549019607843137, 0.9764705882, 0.9333333333, 0.0039215686, 0.8588235294117647, 0.9725490196, 0.9490196078, 0.0039215686, 0.8627450980392157, 0.9725490196, 0.9647058824, 0.0039215686, 0.8666666666666667, 0.9725490196, 0.9803921569, 0.0039215686, 0.8705882352941177, 0.9725490196, 0.9960784314, 0.0039215686, 0.8745098039215686, 0.9725490196, 0.9960784314, 0.0039215686, 0.8784313725490196, 0.9725490196, 0.9960784314, 0.0352941176, 0.8823529411764706, 0.9725490196, 0.9960784314, 0.0666666667, 0.8862745098039215, 0.9725490196, 0.9960784314, 0.0980392157, 0.8901960784313725, 0.9725490196, 0.9960784314, 0.1294117647, 0.8941176470588236, 0.9725490196, 0.9960784314, 0.1647058824, 0.8980392156862745, 0.9764705882, 0.9960784314, 0.1960784314, 0.9019607843137255, 0.9764705882, 0.9960784314, 0.2274509804, 0.9058823529411765, 0.9764705882, 0.9960784314, 0.2549019608, 0.9098039215686274, 0.9764705882, 0.9960784314, 0.2901960784, 0.9137254901960784, 0.9764705882, 0.9960784314, 0.3215686275, 0.9176470588235294, 0.9803921569, 0.9960784314, 0.3529411765, 0.9215686274509803, 0.9803921569, 0.9960784314, 0.3843137255, 0.9254901960784314, 0.9803921569, 0.9960784314, 0.4156862745, 0.9294117647058824, 0.9803921569, 0.9960784314, 0.4509803922, 0.9333333333333333, 0.9803921569, 0.9960784314, 0.4823529412, 0.9372549019607843, 0.9843137255, 0.9960784314, 0.5137254902, 0.9411764705882354, 0.9843137255, 0.9960784314, 0.5450980392, 0.9450980392156864, 0.9843137255, 0.9960784314, 0.5803921569, 0.9490196078431372, 0.9843137255, 0.9960784314, 0.6117647059, 0.9529411764705882, 0.9843137255, 0.9960784314, 0.6431372549, 0.9568627450980394, 0.9882352941, 0.9960784314, 0.6745098039, 0.9607843137254903, 0.9882352941, 0.9960784314, 0.7058823529, 0.9647058823529413, 0.9882352941, 0.9960784314, 0.7411764706, 0.9686274509803922, 0.9882352941, 0.9960784314, 0.768627451, 0.9725490196078431, 0.9882352941, 0.9960784314, 0.8, 0.9764705882352941, 0.9921568627, 0.9960784314, 0.831372549, 0.9803921568627451, 0.9921568627, 0.9960784314, 0.8666666667, 0.984313725490196, 0.9921568627, 0.9960784314, 0.8980392157, 0.9882352941176471, 0.9921568627, 0.9960784314, 0.9294117647, 0.9921568627450981, 0.9921568627, 0.9960784314, 0.9607843137, 0.996078431372549, 0.9960784314, 0.9960784314, 0.9607843137, 1.0, 0.9960784314, 0.9960784314, 0.9607843137]
}]);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/init.js




const {
  registerColormap
} = dist_esm.utilities.colormap;
const CORNERSTONE_3D_TOOLS_SOURCE_NAME = 'Cornerstone3DTools';
const CORNERSTONE_3D_TOOLS_SOURCE_VERSION = '0.1';
/**
 *
 * @param {Object} servicesManager
 * @param {Object} configuration
 * @param {Object|Array} configuration.csToolsConfig
 */
function init(_ref) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const {
    measurementService,
    displaySetService,
    cornerstoneViewportService
  } = servicesManager.services;
  (0,esm.addTool)(esm.RectangleROIStartEndThresholdTool);
  const {
    RectangleROIStartEndThreshold
  } = measurementServiceMappings_measurementServiceMappingsFactory(measurementService, displaySetService, cornerstoneViewportService);
  const csTools3DVer1MeasurementSource = measurementService.getSource(CORNERSTONE_3D_TOOLS_SOURCE_NAME, CORNERSTONE_3D_TOOLS_SOURCE_VERSION);
  measurementService.addMapping(csTools3DVer1MeasurementSource, 'RectangleROIStartEndThreshold', RectangleROIStartEndThreshold.matchingCriteria, RectangleROIStartEndThreshold.toAnnotation, RectangleROIStartEndThreshold.toMeasurement);
  colormaps.forEach(registerColormap);
}
// EXTERNAL MODULE: ../../../node_modules/gl-matrix/esm/index.js + 10 modules
var gl_matrix_esm = __webpack_require__(45451);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/getThresholdValue.ts

function getRoiStats(referencedVolume, annotations) {
  // roiStats
  const {
    imageData
  } = referencedVolume;
  const values = imageData.getPointData().getScalars().getData();

  // Todo: add support for other strategies
  const {
    fn,
    baseValue
  } = _getStrategyFn('max');
  let value = baseValue;
  const boundsIJK = esm.utilities.rectangleROITool.getBoundsIJKFromRectangleAnnotations(annotations, referencedVolume);
  const [[iMin, iMax], [jMin, jMax], [kMin, kMax]] = boundsIJK;
  for (let i = iMin; i <= iMax; i++) {
    for (let j = jMin; j <= jMax; j++) {
      for (let k = kMin; k <= kMax; k++) {
        const offset = imageData.computeOffsetIndex([i, j, k]);
        value = fn(values[offset], value);
      }
    }
  }
  return value;
}
function getThresholdValues(annotationUIDs, referencedVolumes, config) {
  if (config.strategy === 'range') {
    return {
      ptLower: Number(config.ptLower),
      ptUpper: Number(config.ptUpper),
      ctLower: Number(config.ctLower),
      ctUpper: Number(config.ctUpper)
    };
  }
  const {
    weight
  } = config;
  const annotations = annotationUIDs.map(annotationUID => esm.annotation.state.getAnnotation(annotationUID));
  const ptValue = getRoiStats(referencedVolumes[0], annotations);
  return {
    ctLower: -Infinity,
    ctUpper: +Infinity,
    ptLower: weight * ptValue,
    ptUpper: +Infinity
  };
}
function _getStrategyFn(statistic) {
  const baseValue = -Infinity;
  const fn = (number, maxValue) => {
    if (number > maxValue) {
      maxValue = number;
    }
    return maxValue;
  };
  return {
    fn,
    baseValue
  };
}
/* harmony default export */ const getThresholdValue = (getThresholdValues);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/calculateSUVPeak.ts


/**
 * This method calculates the SUV peak on a segmented ROI from a reference PET
 * volume. If a rectangle annotation is provided, the peak is calculated within that
 * rectangle. Otherwise, the calculation is performed on the entire volume which
 * will be slower but same result.
 * @param viewport Viewport to use for the calculation
 * @param labelmap Labelmap from which the mask is taken
 * @param referenceVolume PET volume to use for SUV calculation
 * @param toolData [Optional] list of toolData to use for SUV calculation
 * @param segmentIndex The index of the segment to use for masking
 * @returns
 */
function calculateSuvPeak(labelmap, referenceVolume, annotations) {
  let segmentIndex = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : 1;
  if (referenceVolume.metadata.Modality !== 'PT') {
    return;
  }
  if (labelmap.scalarData.length !== referenceVolume.scalarData.length) {
    throw new Error('labelmap and referenceVolume must have the same number of pixels');
  }
  const {
    scalarData: labelmapData,
    dimensions,
    imageData: labelmapImageData
  } = labelmap;
  const {
    scalarData: referenceVolumeData,
    imageData: referenceVolumeImageData
  } = referenceVolume;
  let boundsIJK;
  // Todo: using the first annotation for now
  if (annotations && annotations[0].data?.cachedStats) {
    const {
      projectionPoints
    } = annotations[0].data.cachedStats;
    const pointsToUse = [].concat(...projectionPoints); // cannot use flat() because of typescript compiler right now

    const rectangleCornersIJK = pointsToUse.map(world => {
      const ijk = gl_matrix_esm/* vec3.fromValues */.R3.fromValues(0, 0, 0);
      referenceVolumeImageData.worldToIndex(world, ijk);
      return ijk;
    });
    boundsIJK = esm.utilities.boundingBox.getBoundingBoxAroundShape(rectangleCornersIJK, dimensions);
  }
  let max = 0;
  let maxIJK = [0, 0, 0];
  let maxLPS = [0, 0, 0];
  const callback = _ref => {
    let {
      pointIJK,
      pointLPS
    } = _ref;
    const offset = referenceVolumeImageData.computeOffsetIndex(pointIJK);
    const value = labelmapData[offset];
    if (value !== segmentIndex) {
      return;
    }
    const referenceValue = referenceVolumeData[offset];
    if (referenceValue > max) {
      max = referenceValue;
      maxIJK = pointIJK;
      maxLPS = pointLPS;
    }
  };
  esm.utilities.pointInShapeCallback(labelmapImageData, () => true, callback, boundsIJK);
  const direction = labelmapImageData.getDirection().slice(0, 3);

  /**
   * 2. Find the bottom and top of the great circle for the second sphere (1cc sphere)
   * V = (4/3)πr3
   */
  const radius = Math.pow(1 / (4 / 3 * Math.PI), 1 / 3) * 10;
  const diameter = radius * 2;
  const secondaryCircleWorld = gl_matrix_esm/* vec3.create */.R3.create();
  const bottomWorld = gl_matrix_esm/* vec3.create */.R3.create();
  const topWorld = gl_matrix_esm/* vec3.create */.R3.create();
  referenceVolumeImageData.indexToWorld(maxIJK, secondaryCircleWorld);
  gl_matrix_esm/* vec3.scaleAndAdd */.R3.scaleAndAdd(bottomWorld, secondaryCircleWorld, direction, -diameter / 2);
  gl_matrix_esm/* vec3.scaleAndAdd */.R3.scaleAndAdd(topWorld, secondaryCircleWorld, direction, diameter / 2);
  const suvPeakCirclePoints = [bottomWorld, topWorld];

  /**
   * 3. Find the Mean and Max of the 1cc sphere centered on the suv Max of the previous
   * sphere
   */
  let count = 0;
  let acc = 0;
  const suvPeakMeanCallback = _ref2 => {
    let {
      value
    } = _ref2;
    acc += value;
    count += 1;
  };
  esm.utilities.pointInSurroundingSphereCallback(referenceVolumeImageData, suvPeakCirclePoints, suvPeakMeanCallback);
  const mean = acc / count;
  return {
    max,
    maxIJK,
    maxLPS,
    mean
  };
}
/* harmony default export */ const calculateSUVPeak = (calculateSuvPeak);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/calculateTMTV.ts


/**
 * Given a list of labelmaps (with the possibility of overlapping regions),
 * and a referenceVolume, it calculates the total metabolic tumor volume (TMTV)
 * by flattening and rasterizing each segment into a single labelmap and summing
 * the total number of volume voxels. It should be noted that for this calculation
 * we do not double count voxels that are part of multiple labelmaps.
 * @param {} labelmaps
 * @param {number} segmentIndex
 * @returns {number} TMTV in ml
 */
function calculateTMTV(labelmaps) {
  let segmentIndex = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 1;
  const volumeId = 'mergedLabelmap';
  const mergedLabelmap = esm.utilities.segmentation.createMergedLabelmapForIndex(labelmaps, segmentIndex, volumeId);
  const {
    imageData,
    spacing
  } = mergedLabelmap;
  const values = imageData.getPointData().getScalars().getData();

  // count non-zero values inside the outputData, this would
  // consider the overlapping regions to be only counted once
  const numVoxels = values.reduce((acc, curr) => {
    if (curr > 0) {
      return acc + 1;
    }
    return acc;
  }, 0);
  return 1e-3 * numVoxels * spacing[0] * spacing[1] * spacing[2];
}
/* harmony default export */ const utils_calculateTMTV = (calculateTMTV);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/createAndDownloadTMTVReport.js
function createAndDownloadTMTVReport(segReport, additionalReportRows) {
  const firstReport = segReport[Object.keys(segReport)[0]];
  const columns = Object.keys(firstReport);
  const csv = [columns.join(',')];
  Object.values(segReport).forEach(segmentation => {
    const row = [];
    columns.forEach(column => {
      // if it is array then we need to replace , with space to avoid csv parsing error
      row.push(Array.isArray(segmentation[column]) ? segmentation[column].join(' ') : segmentation[column]);
    });
    csv.push(row.join(','));
  });
  csv.push('');
  csv.push('');
  csv.push('');
  csv.push(`Patient ID,${firstReport.PatientID}`);
  csv.push(`Study Date,${firstReport.StudyDate}`);
  csv.push('');
  additionalReportRows.forEach(_ref => {
    let {
      key,
      value: values
    } = _ref;
    const temp = [];
    temp.push(`${key}`);
    Object.keys(values).forEach(k => {
      temp.push(`${k}`);
      temp.push(`${values[k]}`);
    });
    csv.push(temp.join(','));
  });
  const blob = new Blob([csv.join('\n')], {
    type: 'text/csv;charset=utf-8'
  });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${firstReport.PatientID}_tmtv.csv`;
  a.click();
}
// EXTERNAL MODULE: ../../../node_modules/dcmjs/build/dcmjs.es.js
var dcmjs_es = __webpack_require__(67540);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/adapters/dist/adapters.es.js
var adapters_es = __webpack_require__(91202);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/dicomRTAnnotationExport/RTStructureSet/dicomRTAnnotationExport.js



const {
  datasetToBlob
} = dcmjs_es["default"].data;
const metadataProvider = core_src.classes.MetadataProvider;
function dicomRTAnnotationExport(annotations) {
  const dataset = adapters_es.adaptersRT.Cornerstone3D.RTSS.generateRTSSFromAnnotations(annotations, metadataProvider, core_src.DicomMetadataStore);
  const reportBlob = datasetToBlob(dataset);

  //Create a URL for the binary.
  var objectUrl = URL.createObjectURL(reportBlob);
  window.location.assign(objectUrl);
}
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/utils/dicomRTAnnotationExport/RTStructureSet/index.js

/* harmony default export */ const RTStructureSet = (dicomRTAnnotationExport);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/commandsModule.js










const commandsModule_metadataProvider = core_src.classes.MetadataProvider;
const RECTANGLE_ROI_THRESHOLD_MANUAL = 'RectangleROIStartEndThreshold';
const LABELMAP = esm.Enums.SegmentationRepresentations.Labelmap;
const commandsModule = _ref => {
  let {
    servicesManager,
    commandsManager,
    extensionManager
  } = _ref;
  const {
    viewportGridService,
    uiNotificationService,
    displaySetService,
    hangingProtocolService,
    toolGroupService,
    cornerstoneViewportService,
    segmentationService
  } = servicesManager.services;
  const utilityModule = extensionManager.getModuleEntry('@ohif/extension-cornerstone.utilityModule.common');
  const {
    getEnabledElement
  } = utilityModule.exports;
  function _getActiveViewportsEnabledElement() {
    const {
      activeViewportId
    } = viewportGridService.getState();
    const {
      element
    } = getEnabledElement(activeViewportId) || {};
    const enabledElement = dist_esm.getEnabledElement(element);
    return enabledElement;
  }
  function _getMatchedViewportsToolGroupIds() {
    const {
      viewportMatchDetails
    } = hangingProtocolService.getMatchDetails();
    const toolGroupIds = [];
    viewportMatchDetails.forEach(viewport => {
      const {
        viewportOptions
      } = viewport;
      const {
        toolGroupId
      } = viewportOptions;
      if (toolGroupIds.indexOf(toolGroupId) === -1) {
        toolGroupIds.push(toolGroupId);
      }
    });
    return toolGroupIds;
  }
  const actions = {
    getMatchingPTDisplaySet: _ref2 => {
      let {
        viewportMatchDetails
      } = _ref2;
      // Todo: this is assuming that the hanging protocol has successfully matched
      // the correct PT. For future, we should have a way to filter out the PTs
      // that are in the viewer layout (but then we have the problem of the attenuation
      // corrected PT vs the non-attenuation correct PT)

      let ptDisplaySet = null;
      for (const [viewportId, viewportDetails] of viewportMatchDetails) {
        const {
          displaySetsInfo
        } = viewportDetails;
        const displaySets = displaySetsInfo.map(_ref3 => {
          let {
            displaySetInstanceUID
          } = _ref3;
          return displaySetService.getDisplaySetByUID(displaySetInstanceUID);
        });
        if (!displaySets || displaySets.length === 0) {
          continue;
        }
        ptDisplaySet = displaySets.find(displaySet => displaySet.Modality === 'PT');
        if (ptDisplaySet) {
          break;
        }
      }
      return ptDisplaySet;
    },
    getPTMetadata: _ref4 => {
      let {
        ptDisplaySet
      } = _ref4;
      const dataSource = extensionManager.getDataSources()[0];
      const imageIds = dataSource.getImageIdsForDisplaySet(ptDisplaySet);
      const firstImageId = imageIds[0];
      const instance = commandsModule_metadataProvider.get('instance', firstImageId);
      if (instance.Modality !== 'PT') {
        return;
      }
      const metadata = {
        SeriesTime: instance.SeriesTime,
        Modality: instance.Modality,
        PatientSex: instance.PatientSex,
        PatientWeight: instance.PatientWeight,
        RadiopharmaceuticalInformationSequence: {
          RadionuclideTotalDose: instance.RadiopharmaceuticalInformationSequence[0].RadionuclideTotalDose,
          RadionuclideHalfLife: instance.RadiopharmaceuticalInformationSequence[0].RadionuclideHalfLife,
          RadiopharmaceuticalStartTime: instance.RadiopharmaceuticalInformationSequence[0].RadiopharmaceuticalStartTime,
          RadiopharmaceuticalStartDateTime: instance.RadiopharmaceuticalInformationSequence[0].RadiopharmaceuticalStartDateTime
        }
      };
      return metadata;
    },
    createNewLabelmapFromPT: async () => {
      // Create a segmentation of the same resolution as the source data
      // using volumeLoader.createAndCacheDerivedVolume.
      const {
        viewportMatchDetails
      } = hangingProtocolService.getMatchDetails();
      const ptDisplaySet = actions.getMatchingPTDisplaySet({
        viewportMatchDetails
      });
      if (!ptDisplaySet) {
        uiNotificationService.error('No matching PT display set found');
        return;
      }
      const segmentationId = await segmentationService.createSegmentationForDisplaySet(ptDisplaySet.displaySetInstanceUID);

      // Add Segmentation to all toolGroupIds in the viewer
      const toolGroupIds = _getMatchedViewportsToolGroupIds();
      const representationType = LABELMAP;
      for (const toolGroupId of toolGroupIds) {
        const hydrateSegmentation = true;
        await segmentationService.addSegmentationRepresentationToToolGroup(toolGroupId, segmentationId, hydrateSegmentation, representationType);
        segmentationService.setActiveSegmentationForToolGroup(segmentationId, toolGroupId);
      }
      return segmentationId;
    },
    setSegmentationActiveForToolGroups: _ref5 => {
      let {
        segmentationId
      } = _ref5;
      const toolGroupIds = _getMatchedViewportsToolGroupIds();
      toolGroupIds.forEach(toolGroupId => {
        segmentationService.setActiveSegmentationForToolGroup(segmentationId, toolGroupId);
      });
    },
    thresholdSegmentationByRectangleROITool: _ref6 => {
      let {
        segmentationId,
        config
      } = _ref6;
      const segmentation = esm.segmentation.state.getSegmentation(segmentationId);
      const {
        representationData
      } = segmentation;
      const {
        displaySetMatchDetails: matchDetails
      } = hangingProtocolService.getMatchDetails();
      const volumeLoaderScheme = 'cornerstoneStreamingImageVolume'; // Loader id which defines which volume loader to use

      const ctDisplaySet = matchDetails.get('ctDisplaySet');
      const ctVolumeId = `${volumeLoaderScheme}:${ctDisplaySet.displaySetInstanceUID}`; // VolumeId with loader id + volume id

      const {
        volumeId: segVolumeId
      } = representationData[LABELMAP];
      const {
        referencedVolumeId
      } = dist_esm.cache.getVolume(segVolumeId);
      const labelmapVolume = dist_esm.cache.getVolume(segmentationId);
      const referencedVolume = dist_esm.cache.getVolume(referencedVolumeId);
      const ctReferencedVolume = dist_esm.cache.getVolume(ctVolumeId);
      if (!referencedVolume) {
        throw new Error('No Reference volume found');
      }
      if (!labelmapVolume) {
        throw new Error('No Reference labelmap found');
      }
      const annotationUIDs = esm.annotation.selection.getAnnotationsSelectedByToolName(RECTANGLE_ROI_THRESHOLD_MANUAL);
      if (annotationUIDs.length === 0) {
        uiNotificationService.show({
          title: 'Commands Module',
          message: 'No ROIThreshold Tool is Selected',
          type: 'error'
        });
        return;
      }
      const {
        ptLower,
        ptUpper,
        ctLower,
        ctUpper
      } = getThresholdValue(annotationUIDs, [referencedVolume, ctReferencedVolume], config);
      return esm.utilities.segmentation.rectangleROIThresholdVolumeByRange(annotationUIDs, labelmapVolume, [{
        volume: referencedVolume,
        lower: ptLower,
        upper: ptUpper
      }, {
        volume: ctReferencedVolume,
        lower: ctLower,
        upper: ctUpper
      }], {
        overwrite: true
      });
    },
    calculateSuvPeak: _ref7 => {
      let {
        labelmap
      } = _ref7;
      const {
        referencedVolumeId
      } = labelmap;
      const referencedVolume = dist_esm.cache.getVolume(referencedVolumeId);
      const annotationUIDs = esm.annotation.selection.getAnnotationsSelectedByToolName(RECTANGLE_ROI_THRESHOLD_MANUAL);
      const annotations = annotationUIDs.map(annotationUID => esm.annotation.state.getAnnotation(annotationUID));
      const suvPeak = calculateSUVPeak(labelmap, referencedVolume, annotations);
      return {
        suvPeak: suvPeak.mean,
        suvMax: suvPeak.max,
        suvMaxIJK: suvPeak.maxIJK,
        suvMaxLPS: suvPeak.maxLPS
      };
    },
    getLesionStats: _ref8 => {
      let {
        labelmap,
        segmentIndex = 1
      } = _ref8;
      const {
        scalarData,
        spacing
      } = labelmap;
      const {
        scalarData: referencedScalarData
      } = dist_esm.cache.getVolume(labelmap.referencedVolumeId);
      let segmentationMax = -Infinity;
      let segmentationMin = Infinity;
      let segmentationValues = [];
      let voxelCount = 0;
      for (let i = 0; i < scalarData.length; i++) {
        if (scalarData[i] === segmentIndex) {
          const value = referencedScalarData[i];
          segmentationValues.push(value);
          if (value > segmentationMax) {
            segmentationMax = value;
          }
          if (value < segmentationMin) {
            segmentationMin = value;
          }
          voxelCount++;
        }
      }
      const stats = {
        minValue: segmentationMin,
        maxValue: segmentationMax,
        meanValue: segmentationValues.reduce((a, b) => a + b, 0) / voxelCount,
        stdValue: Math.sqrt(segmentationValues.reduce((a, b) => a + b * b, 0) / voxelCount - segmentationValues.reduce((a, b) => a + b, 0) / voxelCount ** 2),
        volume: voxelCount * spacing[0] * spacing[1] * spacing[2] * 1e-3
      };
      return stats;
    },
    calculateLesionGlycolysis: _ref9 => {
      let {
        lesionStats
      } = _ref9;
      const {
        meanValue,
        volume
      } = lesionStats;
      return {
        lesionGlyoclysisStats: volume * meanValue
      };
    },
    calculateTMTV: _ref10 => {
      let {
        segmentations
      } = _ref10;
      const labelmaps = segmentations.map(s => segmentationService.getLabelmapVolume(s.id));
      if (!labelmaps.length) {
        return;
      }
      return utils_calculateTMTV(labelmaps);
    },
    exportTMTVReportCSV: _ref11 => {
      let {
        segmentations,
        tmtv,
        config
      } = _ref11;
      const segReport = commandsManager.runCommand('getSegmentationCSVReport', {
        segmentations
      });
      const tlg = actions.getTotalLesionGlycolysis({
        segmentations
      });
      const additionalReportRows = [{
        key: 'Total Metabolic Tumor Volume',
        value: {
          tmtv
        }
      }, {
        key: 'Total Lesion Glycolysis',
        value: {
          tlg: tlg.toFixed(4)
        }
      }, {
        key: 'Threshold Configuration',
        value: {
          ...config
        }
      }];
      createAndDownloadTMTVReport(segReport, additionalReportRows);
    },
    getTotalLesionGlycolysis: _ref12 => {
      let {
        segmentations
      } = _ref12;
      const labelmapVolumes = segmentations.map(s => segmentationService.getLabelmapVolume(s.id));
      let mergedLabelmap;
      // merge labelmap will through an error if labels maps are not the same size
      // or same direction or ....
      try {
        mergedLabelmap = esm.utilities.segmentation.createMergedLabelmapForIndex(labelmapVolumes);
      } catch (e) {
        console.error('commandsModule::getTotalLesionGlycolysis', e);
        return;
      }

      // grabbing the first labelmap referenceVolume since it will be the same for all
      const {
        referencedVolumeId,
        spacing
      } = labelmapVolumes[0];
      if (!referencedVolumeId) {
        console.error('commandsModule::getTotalLesionGlycolysis:No referencedVolumeId found');
      }
      const ptVolume = dist_esm.cache.getVolume(referencedVolumeId);
      const mergedLabelData = mergedLabelmap.scalarData;
      if (mergedLabelData.length !== ptVolume.scalarData.length) {
        console.error('commandsModule::getTotalLesionGlycolysis:Labelmap and ptVolume are not the same size');
      }
      let suv = 0;
      let totalLesionVoxelCount = 0;
      for (let i = 0; i < mergedLabelData.length; i++) {
        // if not background
        if (mergedLabelData[i] !== 0) {
          suv += ptVolume.scalarData[i];
          totalLesionVoxelCount += 1;
        }
      }

      // Average SUV for the merged labelmap
      const averageSuv = suv / totalLesionVoxelCount;

      // total Lesion Glycolysis [suv * ml]
      return averageSuv * totalLesionVoxelCount * spacing[0] * spacing[1] * spacing[2] * 1e-3;
    },
    setStartSliceForROIThresholdTool: () => {
      const {
        viewport
      } = _getActiveViewportsEnabledElement();
      const {
        focalPoint,
        viewPlaneNormal
      } = viewport.getCamera();
      const selectedAnnotationUIDs = esm.annotation.selection.getAnnotationsSelectedByToolName(RECTANGLE_ROI_THRESHOLD_MANUAL);
      const annotationUID = selectedAnnotationUIDs[0];
      const annotation = esm.annotation.state.getAnnotation(annotationUID);
      const {
        handles
      } = annotation.data;
      const {
        points
      } = handles;

      // get the current slice Index
      const sliceIndex = viewport.getCurrentImageIdIndex();
      annotation.data.startSlice = sliceIndex;

      // distance between camera focal point and each point on the rectangle
      const newPoints = points.map(point => {
        const distance = gl_matrix_esm/* vec3.create */.R3.create();
        gl_matrix_esm/* vec3.subtract */.R3.subtract(distance, focalPoint, point);
        // distance in the direction of the viewPlaneNormal
        const distanceInViewPlane = gl_matrix_esm/* vec3.dot */.R3.dot(distance, viewPlaneNormal);
        // new point is current point minus distanceInViewPlane
        const newPoint = gl_matrix_esm/* vec3.create */.R3.create();
        gl_matrix_esm/* vec3.scaleAndAdd */.R3.scaleAndAdd(newPoint, point, viewPlaneNormal, distanceInViewPlane);
        return newPoint;
        //
      });

      handles.points = newPoints;
      // IMPORTANT: invalidate the toolData for the cached stat to get updated
      // and re-calculate the projection points
      annotation.invalidated = true;
      viewport.render();
    },
    setEndSliceForROIThresholdTool: () => {
      const {
        viewport
      } = _getActiveViewportsEnabledElement();
      const selectedAnnotationUIDs = esm.annotation.selection.getAnnotationsSelectedByToolName(RECTANGLE_ROI_THRESHOLD_MANUAL);
      const annotationUID = selectedAnnotationUIDs[0];
      const annotation = esm.annotation.state.getAnnotation(annotationUID);

      // get the current slice Index
      const sliceIndex = viewport.getCurrentImageIdIndex();
      annotation.data.endSlice = sliceIndex;

      // IMPORTANT: invalidate the toolData for the cached stat to get updated
      // and re-calculate the projection points
      annotation.invalidated = true;
      viewport.render();
    },
    createTMTVRTReport: () => {
      // get all Rectangle ROI annotation
      const stateManager = esm.annotation.state.getAnnotationManager();
      const annotations = [];
      Object.keys(stateManager.annotations).forEach(frameOfReferenceUID => {
        const forAnnotations = stateManager.annotations[frameOfReferenceUID];
        const ROIAnnotations = forAnnotations[RECTANGLE_ROI_THRESHOLD_MANUAL];
        annotations.push(...ROIAnnotations);
      });
      commandsManager.runCommand('exportRTReportForAnnotations', {
        annotations
      });
    },
    getSegmentationCSVReport: _ref13 => {
      let {
        segmentations
      } = _ref13;
      if (!segmentations || !segmentations.length) {
        segmentations = segmentationService.getSegmentations();
      }
      let report = {};
      for (const segmentation of segmentations) {
        const {
          id,
          label,
          cachedStats: data
        } = segmentation;
        const segReport = {
          id,
          label
        };
        if (!data) {
          report[id] = segReport;
          continue;
        }
        Object.keys(data).forEach(key => {
          if (typeof data[key] !== 'object') {
            segReport[key] = data[key];
          } else {
            Object.keys(data[key]).forEach(subKey => {
              const newKey = `${key}_${subKey}`;
              segReport[newKey] = data[key][subKey];
            });
          }
        });
        const labelmapVolume = segmentationService.getLabelmapVolume(id);
        if (!labelmapVolume) {
          report[id] = segReport;
          continue;
        }
        const referencedVolumeId = labelmapVolume.referencedVolumeId;
        segReport.referencedVolumeId = referencedVolumeId;
        const referencedVolume = segmentationService.getLabelmapVolume(referencedVolumeId);
        if (!referencedVolume) {
          report[id] = segReport;
          continue;
        }
        if (!referencedVolume.imageIds || !referencedVolume.imageIds.length) {
          report[id] = segReport;
          continue;
        }
        const firstImageId = referencedVolume.imageIds[0];
        const instance = core_src["default"].classes.MetadataProvider.get('instance', firstImageId);
        if (!instance) {
          report[id] = segReport;
          continue;
        }
        report[id] = {
          ...segReport,
          PatientID: instance.PatientID,
          PatientName: instance.PatientName.Alphabetic,
          StudyInstanceUID: instance.StudyInstanceUID,
          SeriesInstanceUID: instance.SeriesInstanceUID,
          StudyDate: instance.StudyDate
        };
      }
      return report;
    },
    exportRTReportForAnnotations: _ref14 => {
      let {
        annotations
      } = _ref14;
      RTStructureSet(annotations);
    },
    setFusionPTColormap: _ref15 => {
      let {
        toolGroupId,
        colormap
      } = _ref15;
      const toolGroup = toolGroupService.getToolGroup(toolGroupId);
      const {
        viewportMatchDetails
      } = hangingProtocolService.getMatchDetails();
      const ptDisplaySet = actions.getMatchingPTDisplaySet({
        viewportMatchDetails
      });
      if (!ptDisplaySet) {
        return;
      }
      const fusionViewportIds = toolGroup.getViewportIds();
      let viewports = [];
      fusionViewportIds.forEach(viewportId => {
        commandsManager.runCommand('setViewportColormap', {
          viewportId,
          displaySetInstanceUID: ptDisplaySet.displaySetInstanceUID,
          colormap: {
            name: colormap,
            // TODO: This opacity mapping matches that in hpViewports, but
            // ideally making this editable in a side panel would be useful
            opacity: [{
              value: 0,
              opacity: 0
            }, {
              value: 0.1,
              opacity: 0.9
            }, {
              value: 1,
              opacity: 0.95
            }]
          }
        });
        viewports.push(cornerstoneViewportService.getCornerstoneViewport(viewportId));
      });
      viewports.forEach(viewport => {
        viewport.render();
      });
    }
  };
  const definitions = {
    setEndSliceForROIThresholdTool: {
      commandFn: actions.setEndSliceForROIThresholdTool,
      storeContexts: [],
      options: {}
    },
    setStartSliceForROIThresholdTool: {
      commandFn: actions.setStartSliceForROIThresholdTool,
      storeContexts: [],
      options: {}
    },
    getMatchingPTDisplaySet: {
      commandFn: actions.getMatchingPTDisplaySet,
      storeContexts: [],
      options: {}
    },
    getPTMetadata: {
      commandFn: actions.getPTMetadata,
      storeContexts: [],
      options: {}
    },
    createNewLabelmapFromPT: {
      commandFn: actions.createNewLabelmapFromPT,
      storeContexts: [],
      options: {}
    },
    setSegmentationActiveForToolGroups: {
      commandFn: actions.setSegmentationActiveForToolGroups,
      storeContexts: [],
      options: {}
    },
    thresholdSegmentationByRectangleROITool: {
      commandFn: actions.thresholdSegmentationByRectangleROITool,
      storeContexts: [],
      options: {}
    },
    getTotalLesionGlycolysis: {
      commandFn: actions.getTotalLesionGlycolysis,
      storeContexts: [],
      options: {}
    },
    calculateSuvPeak: {
      commandFn: actions.calculateSuvPeak,
      storeContexts: [],
      options: {}
    },
    getLesionStats: {
      commandFn: actions.getLesionStats,
      storeContexts: [],
      options: {}
    },
    calculateTMTV: {
      commandFn: actions.calculateTMTV,
      storeContexts: [],
      options: {}
    },
    exportTMTVReportCSV: {
      commandFn: actions.exportTMTVReportCSV,
      storeContexts: [],
      options: {}
    },
    createTMTVRTReport: {
      commandFn: actions.createTMTVRTReport,
      storeContexts: [],
      options: {}
    },
    getSegmentationCSVReport: {
      commandFn: actions.getSegmentationCSVReport,
      storeContexts: [],
      options: {}
    },
    exportRTReportForAnnotations: {
      commandFn: actions.exportRTReportForAnnotations,
      storeContexts: [],
      options: {}
    },
    setFusionPTColormap: {
      commandFn: actions.setFusionPTColormap,
      storeContexts: [],
      options: {}
    }
  };
  return {
    actions,
    definitions,
    defaultContext: 'TMTV:CORNERSTONE'
  };
};
/* harmony default export */ const src_commandsModule = (commandsModule);
;// CONCATENATED MODULE: ../../../extensions/tmtv/src/index.tsx






/**
 *
 */
const tmtvExtension = {
  /**
   * Only required property. Should be a unique value across all extensions.
   */
  id: id,
  preRegistration(_ref) {
    let {
      servicesManager,
      commandsManager,
      extensionManager,
      configuration = {}
    } = _ref;
    init({
      servicesManager,
      commandsManager,
      extensionManager,
      configuration
    });
  },
  getPanelModule: src_getPanelModule,
  getHangingProtocolModule: src_getHangingProtocolModule,
  getCommandsModule(_ref2) {
    let {
      servicesManager,
      commandsManager,
      extensionManager
    } = _ref2;
    return src_commandsModule({
      servicesManager,
      commandsManager,
      extensionManager
    });
  }
};
/* harmony default export */ const tmtv_src = (tmtvExtension);

/***/ }),

/***/ 78753:
/***/ (() => {

/* (ignored) */

/***/ })

}]);