"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[814],{

/***/ 92814:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ dicom_pdf_src)
});

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
;// CONCATENATED MODULE: ../../../extensions/dicom-pdf/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-dicom-pdf"}');
;// CONCATENATED MODULE: ../../../extensions/dicom-pdf/src/id.js

const id = package_namespaceObject.u2;
const SOPClassHandlerId = `${id}.sopClassHandlerModule.dicom-pdf`;

// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var src = __webpack_require__(71771);
;// CONCATENATED MODULE: ../../../extensions/dicom-pdf/src/getSopClassHandlerModule.js


const {
  ImageSet
} = src.classes;
const SOP_CLASS_UIDS = {
  ENCAPSULATED_PDF: '1.2.840.10008.5.1.4.1.1.104.1'
};
const sopClassUids = Object.values(SOP_CLASS_UIDS);
const _getDisplaySetsFromSeries = (instances, servicesManager, extensionManager) => {
  const dataSource = extensionManager.getActiveDataSource()[0];
  return instances.map(instance => {
    const {
      Modality,
      SOPInstanceUID,
      EncapsulatedDocument
    } = instance;
    const {
      SeriesDescription = 'PDF',
      MIMETypeOfEncapsulatedDocument
    } = instance;
    const {
      SeriesNumber,
      SeriesDate,
      SeriesInstanceUID,
      StudyInstanceUID,
      SOPClassUID
    } = instance;
    const pdfUrl = dataSource.retrieve.directURL({
      instance,
      tag: 'EncapsulatedDocument',
      defaultType: MIMETypeOfEncapsulatedDocument || 'application/pdf',
      singlepart: 'pdf'
    });
    const displaySet = {
      //plugin: id,
      Modality,
      displaySetInstanceUID: src.utils.guid(),
      SeriesDescription,
      SeriesNumber,
      SeriesDate,
      SOPInstanceUID,
      SeriesInstanceUID,
      StudyInstanceUID,
      SOPClassHandlerId: SOPClassHandlerId,
      SOPClassUID,
      referencedImages: null,
      measurements: null,
      pdfUrl,
      instances: [instance],
      thumbnailSrc: dataSource.retrieve.directURL({
        instance,
        defaultPath: '/thumbnail',
        defaultType: 'image/jpeg',
        tag: 'Absent'
      }),
      isDerivedDisplaySet: true,
      isLoaded: false,
      sopClassUids,
      numImageFrames: 0,
      numInstances: 1,
      instance
    };
    return displaySet;
  });
};
function getSopClassHandlerModule(_ref) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const getDisplaySetsFromSeries = instances => {
    return _getDisplaySetsFromSeries(instances, servicesManager, extensionManager);
  };
  return [{
    name: 'dicom-pdf',
    sopClassUids,
    getDisplaySetsFromSeries
  }];
}
;// CONCATENATED MODULE: ../../../extensions/dicom-pdf/src/index.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }



const Component = /*#__PURE__*/react.lazy(() => {
  return __webpack_require__.e(/* import() */ 125).then(__webpack_require__.bind(__webpack_require__, 39125));
});
const OHIFCornerstonePdfViewport = props => {
  return /*#__PURE__*/react.createElement(react.Suspense, {
    fallback: /*#__PURE__*/react.createElement("div", null, "Loading...")
  }, /*#__PURE__*/react.createElement(Component, props));
};

/**
 *
 */
const dicomPDFExtension = {
  /**
   * Only required property. Should be a unique value across all extensions.
   */
  id: id,
  /**
   *
   *
   * @param {object} [configuration={}]
   * @param {object|array} [configuration.csToolsConfig] - Passed directly to `initCornerstoneTools`
   */
  getViewportModule(_ref) {
    let {
      servicesManager,
      extensionManager
    } = _ref;
    const ExtendedOHIFCornerstonePdfViewport = props => {
      return /*#__PURE__*/react.createElement(OHIFCornerstonePdfViewport, _extends({
        servicesManager: servicesManager,
        extensionManager: extensionManager
      }, props));
    };
    return [{
      name: 'dicom-pdf',
      component: ExtendedOHIFCornerstonePdfViewport
    }];
  },
  // getCommandsModule({ servicesManager }) {
  //   return {
  //     definitions: {
  //       setToolActive: {
  //         commandFn: () => null,
  //         storeContexts: [],
  //         options: {},
  //       },
  //     },
  //     defaultContext: 'ACTIVE_VIEWPORT::PDF',
  // };
  // },
  getSopClassHandlerModule: getSopClassHandlerModule
};
/* harmony default export */ const dicom_pdf_src = (dicomPDFExtension);

/***/ })

}]);