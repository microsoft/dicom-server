"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[250,579],{

/***/ 76516:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ src_DicomMicroscopyViewport)
});

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../../node_modules/react-resize-detector/build/index.esm.js
var index_esm = __webpack_require__(7023);
// EXTERNAL MODULE: ../../../node_modules/prop-types/index.js
var prop_types = __webpack_require__(3827);
var prop_types_default = /*#__PURE__*/__webpack_require__.n(prop_types);
// EXTERNAL MODULE: ../../../node_modules/lodash.debounce/index.js
var lodash_debounce = __webpack_require__(8324);
var lodash_debounce_default = /*#__PURE__*/__webpack_require__.n(lodash_debounce);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var src = __webpack_require__(71783);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/DicomMicroscopyViewport.css
// extracted by mini-css-extract-plugin

// EXTERNAL MODULE: ../../../node_modules/classnames/index.js
var classnames = __webpack_require__(44921);
var classnames_default = /*#__PURE__*/__webpack_require__.n(classnames);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/components/ViewportOverlay/listComponentGenerator.tsx
const listComponentGenerator = props => {
  const {
    list,
    itemGenerator
  } = props;
  if (!list) {
    return;
  }
  return list.map(item => {
    if (!item) {
      return;
    }
    const generator = item.generator || itemGenerator;
    if (!generator) {
      throw new Error(`No generator for ${item}`);
    }
    return generator({
      ...props,
      item
    });
  });
};
/* harmony default export */ const ViewportOverlay_listComponentGenerator = (listComponentGenerator);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/components/ViewportOverlay/ViewportOverlay.css
// extracted by mini-css-extract-plugin

// EXTERNAL MODULE: ../../../node_modules/moment/moment.js
var moment_moment = __webpack_require__(71271);
// EXTERNAL MODULE: ../../../node_modules/@cornerstonejs/core/dist/esm/index.js + 331 modules
var esm = __webpack_require__(3743);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/components/ViewportOverlay/utils.ts



/**
 * Checks if value is valid.
 *
 * @param {number} value
 * @returns {boolean} is valid.
 */
function isValidNumber(value) {
  return typeof value === 'number' && !isNaN(value);
}

/**
 * Formats number precision.
 *
 * @param {number} number
 * @param {number} precision
 * @returns {number} formatted number.
 */
function utils_formatNumberPrecision(number, precision) {
  if (number !== null) {
    return parseFloat(number).toFixed(precision);
  }
}

/**
 * Formats DICOM date.
 *
 * @param {string} date
 * @param {string} strFormat
 * @returns {string} formatted date.
 */
function utils_formatDICOMDate(date) {
  let strFormat = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 'MMM D, YYYY';
  return moment(date, 'YYYYMMDD').format(strFormat);
}

/**
 *    DICOM Time is stored as HHmmss.SSS, where:
 *      HH 24 hour time:
 *        m mm        0..59   Minutes
 *        s ss        0..59   Seconds
 *        S SS SSS    0..999  Fractional seconds
 *
 *        Goal: '24:12:12'
 *
 * @param {*} time
 * @param {string} strFormat
 * @returns {string} formatted name.
 */
function utils_formatDICOMTime(time) {
  let strFormat = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 'HH:mm:ss';
  return moment(time, 'HH:mm:ss').format(strFormat);
}

/**
 * Formats a patient name for display purposes
 *
 * @param {string} name
 * @returns {string} formatted name.
 */
function utils_formatPN(name) {
  if (!name) {
    return;
  }

  // Convert the first ^ to a ', '. String.replace() only affects
  // the first appearance of the character.
  const commaBetweenFirstAndLast = name.replace('^', ', ');

  // Replace any remaining '^' characters with spaces
  const cleaned = commaBetweenFirstAndLast.replace(/\^/g, ' ');

  // Trim any extraneous whitespace
  return cleaned.trim();
}

/**
 * Gets compression type
 *
 * @param {number} imageId
 * @returns {string} compression type.
 */
function getCompression(imageId) {
  const generalImageModule = cornerstone.metaData.get('generalImageModule', imageId) || {};
  const {
    lossyImageCompression,
    lossyImageCompressionRatio,
    lossyImageCompressionMethod
  } = generalImageModule;
  if (lossyImageCompression === '01' && lossyImageCompressionRatio !== '') {
    const compressionMethod = lossyImageCompressionMethod || 'Lossy: ';
    const compressionRatio = utils_formatNumberPrecision(lossyImageCompressionRatio, 2);
    return compressionMethod + compressionRatio + ' : 1';
  }
  return 'Lossless / Uncompressed';
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/components/ViewportOverlay/index.tsx





/**
 *
 * @param {*} config is a configuration object that defines four lists of elements,
 * one topLeft, topRight, bottomLeft, bottomRight contents.
 * @param {*} extensionManager is used to load the image data.
 * @returns
 */
const generateFromConfig = _ref => {
  let {
    topLeft = [],
    topRight = [],
    bottomLeft = [],
    bottomRight = [],
    itemGenerator = () => {}
  } = _ref;
  return props => {
    const topLeftClass = 'top-viewport left-viewport text-primary-light';
    const topRightClass = 'top-viewport right-viewport-scrollbar text-primary-light';
    const bottomRightClass = 'bottom-viewport right-viewport-scrollbar text-primary-light';
    const bottomLeftClass = 'bottom-viewport left-viewport text-primary-light';
    const overlay = 'absolute pointer-events-none microscopy-viewport-overlay';
    return /*#__PURE__*/react.createElement(react.Fragment, null, topLeft && topLeft.length > 0 && /*#__PURE__*/react.createElement("div", {
      "data-cy": 'viewport-overlay-top-left',
      className: classnames_default()(overlay, topLeftClass)
    }, ViewportOverlay_listComponentGenerator({
      ...props,
      list: topLeft,
      itemGenerator
    })), topRight && topRight.length > 0 && /*#__PURE__*/react.createElement("div", {
      "data-cy": 'viewport-overlay-top-right',
      className: classnames_default()(overlay, topRightClass)
    }, ViewportOverlay_listComponentGenerator({
      ...props,
      list: topRight,
      itemGenerator
    })), bottomRight && bottomRight.length > 0 && /*#__PURE__*/react.createElement("div", {
      "data-cy": 'viewport-overlay-bottom-right',
      className: classnames_default()(overlay, bottomRightClass)
    }, ViewportOverlay_listComponentGenerator({
      ...props,
      list: bottomRight,
      itemGenerator
    })), bottomLeft && bottomLeft.length > 0 && /*#__PURE__*/react.createElement("div", {
      "data-cy": 'viewport-overlay-bottom-left',
      className: classnames_default()(overlay, bottomLeftClass)
    }, ViewportOverlay_listComponentGenerator({
      ...props,
      list: bottomLeft,
      itemGenerator
    })));
  };
};
const itemGenerator = props => {
  const {
    item
  } = props;
  const {
    title,
    value: valueFunc,
    condition,
    contents
  } = item;
  props.image = {
    ...props.image,
    ...props.metadata
  };
  props.formatDate = formatDICOMDate;
  props.formatTime = formatDICOMTime;
  props.formatPN = formatPN;
  props.formatNumberPrecision = formatNumberPrecision;
  if (condition && !condition(props)) {
    return null;
  }
  if (!contents && !valueFunc) {
    return null;
  }
  const value = valueFunc && valueFunc(props);
  const contentsValue = contents && contents(props) || [{
    className: 'mr-1',
    value: title
  }, {
    classname: 'mr-1 font-light',
    value
  }];
  return /*#__PURE__*/React.createElement("div", {
    key: item.id,
    className: "flex flex-row"
  }, contentsValue.map((content, idx) => /*#__PURE__*/React.createElement("span", {
    key: idx,
    className: content.className
  }, content.value)));
};
/* harmony default export */ const ViewportOverlay = (generateFromConfig({}));
// EXTERNAL MODULE: ../../../node_modules/dicomweb-client/build/dicomweb-client.es.js
var dicomweb_client_es = __webpack_require__(97604);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var core_src = __webpack_require__(71771);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/dicomWebClient.ts


const {
  DICOMwebClient
} = dicomweb_client_es.api;
DICOMwebClient._buildMultipartAcceptHeaderFieldValue = () => {
  return '*/*';
};

/**
 * create a DICOMwebClient object to be used by Dicom Microscopy Viewer
 *
 * Referenced the code from `/extensions/default/src/DicomWebDataSource/index.js`
 *
 * @param param0
 * @returns
 */
function getDicomWebClient(_ref) {
  let {
    extensionManager,
    servicesManager
  } = _ref;
  const dataSourceConfig = window.config.dataSources.find(ds => ds.sourceName === extensionManager.activeDataSource);
  const {
    userAuthenticationService
  } = servicesManager.services;
  const {
    wadoRoot,
    staticWado,
    singlepart
  } = dataSourceConfig.configuration;
  const wadoConfig = {
    url: wadoRoot || '/dicomlocal',
    staticWado,
    singlepart,
    headers: userAuthenticationService.getAuthorizationHeader(),
    errorInterceptor: core_src/* errorHandler */.Po.getHTTPErrorHandler()
  };
  const client = new dicomweb_client_es.api.DICOMwebClient(wadoConfig);
  client.wadoURL = wadoConfig.url;
  if (extensionManager.activeDataSource === 'dicomlocal') {
    /**
     * For local data source, override the retrieveInstanceFrames() method of the
     * dicomweb-client to retrieve image data from memory cached metadata.
     * Other methods of the client doesn't matter, as we are feeding the DMV
     * with the series metadata already.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @param {String} options.frameNumbers - One-based indices of Frame Items
     * @param {Object} [options.queryParams] - HTTP query parameters
     * @returns {ArrayBuffer[]} Rendered Frame Items as byte arrays
     */
    //
    client.retrieveInstanceFrames = async options => {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required for retrieval of instance frames');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required for retrieval of instance frames');
      }
      if (!('sopInstanceUID' in options)) {
        throw new Error('SOP Instance UID is required for retrieval of instance frames');
      }
      if (!('frameNumbers' in options)) {
        throw new Error('frame numbers are required for retrieval of instance frames');
      }
      console.log(`retrieve frames ${options.frameNumbers.toString()} of instance ${options.sopInstanceUID}`);
      const instance = core_src.DicomMetadataStore.getInstance(options.studyInstanceUID, options.seriesInstanceUID, options.sopInstanceUID);
      const frameNumbers = Array.isArray(options.frameNumbers) ? options.frameNumbers : options.frameNumbers.split(',');
      return frameNumbers.map(fr => Array.isArray(instance.PixelData) ? instance.PixelData[+fr - 1] : instance.PixelData);
    };
  }
  return client;
}
// EXTERNAL MODULE: ../../../node_modules/dcmjs/build/dcmjs.es.js
var dcmjs_es = __webpack_require__(67540);
// EXTERNAL MODULE: ../../../extensions/default/src/index.ts + 76 modules
var default_src = __webpack_require__(56342);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/cleanDenaturalizedDataset.ts

function isPrimitive(v) {
  return !(typeof v == 'object' || Array.isArray(v));
}
const vrNumerics = ['DS', 'FL', 'FD', 'IS', 'OD', 'OF', 'OL', 'OV', 'SL', 'SS', 'SV', 'UL', 'US', 'UV'];

/**
 * Specialized for DICOM JSON format dataset cleaning.
 * @param obj
 * @returns
 */
function cleanDenaturalizedDataset(obj, options) {
  if (Array.isArray(obj)) {
    const newAry = obj.map(o => isPrimitive(o) ? o : cleanDenaturalizedDataset(o, options));
    return newAry;
  } else if (isPrimitive(obj)) {
    return obj;
  } else {
    Object.keys(obj).forEach(key => {
      if (obj[key].Value === null && obj[key].vr) {
        delete obj[key].Value;
      } else if (Array.isArray(obj[key].Value) && obj[key].vr) {
        if (obj[key].Value.length === 1 && obj[key].Value[0].BulkDataURI) {
          default_src.dicomWebUtils.fixBulkDataURI(obj[key].Value[0], options, options.dataSourceConfig);
          obj[key].BulkDataURI = obj[key].Value[0].BulkDataURI;

          // prevent mixed-content blockage
          if (window.location.protocol === 'https:' && obj[key].BulkDataURI.startsWith('http:')) {
            obj[key].BulkDataURI = obj[key].BulkDataURI.replace('http:', 'https:');
          }
          delete obj[key].Value;
        } else if (vrNumerics.includes(obj[key].vr)) {
          obj[key].Value = obj[key].Value.map(v => +v);
        } else {
          obj[key].Value = obj[key].Value.map(entry => cleanDenaturalizedDataset(entry, options));
        }
      }
    });
    return obj;
  }
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/DicomMicroscopyViewport.tsx










function transformImageTypeUnnaturalized(entry) {
  if (entry.vr === 'CS') {
    return {
      vr: 'US',
      Value: entry.Value[0].split('\\')
    };
  }
  return entry;
}
class DicomMicroscopyViewport extends react.Component {
  constructor(props) {
    super(props);
    this.state = {
      error: null,
      isLoaded: false
    };
    this.microscopyService = void 0;
    this.viewer = null;
    // dicom-microscopy-viewer instance
    this.managedViewer = null;
    // managed wrapper of microscopy-dicom extension
    this.container = /*#__PURE__*/react.createRef();
    this.overlayElement = /*#__PURE__*/react.createRef();
    this.debouncedResize = void 0;
    this.setViewportActiveHandler = () => {
      const {
        setViewportActive,
        viewportId,
        activeViewportId
      } = this.props;
      if (viewportId !== activeViewportId) {
        setViewportActive(viewportId);
      }
    };
    this.onWindowResize = () => {
      this.debouncedResize();
    };
    const {
      microscopyService
    } = this.props.servicesManager.services;
    this.microscopyService = microscopyService;
    this.debouncedResize = lodash_debounce_default()(() => {
      if (this.viewer) {
        this.viewer.resize();
      }
    }, 100);
  }
  /**
   * Get the nearest ROI from the mouse click point
   *
   * @param event
   * @param autoselect
   * @returns
   */
  getNearbyROI(event) {
    let autoselect = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : true;
    const symbols = Object.getOwnPropertySymbols(this.viewer);
    const _drawingSource = symbols.find(p => p.description === 'drawingSource');
    const _pyramid = symbols.find(p => p.description === 'pyramid');
    const _map = symbols.find(p => p.description === 'map');
    const _affine = symbols.find(p => p.description === 'affine');
    const feature = this.viewer[_drawingSource].getClosestFeatureToCoordinate(this.viewer[_map].getEventCoordinate(event));
    if (!feature) {
      return null;
    }
    const roiAnnotation = this.viewer._getROIFromFeature(feature, this.viewer[_pyramid].metadata, this.viewer[_affine]);
    if (roiAnnotation && autoselect) {
      this.microscopyService.selectAnnotation(roiAnnotation);
    }
    return roiAnnotation;
  }

  // install the microscopy renderer into the web page.
  // you should only do this once.
  async installOpenLayersRenderer(container, displaySet) {
    const loadViewer = async metadata => {
      const {
        viewer: DicomMicroscopyViewer,
        metadata: metadataUtils
      } = await __webpack_require__.e(/* import() | dicom-microscopy-viewer */ 18).then(__webpack_require__.t.bind(__webpack_require__, 42613, 23));
      const microscopyViewer = DicomMicroscopyViewer.VolumeImageViewer;
      const client = getDicomWebClient({
        extensionManager: this.props.extensionManager,
        servicesManager: this.props.servicesManager
      });

      // Parse, format, and filter metadata
      const volumeImages = [];

      /**
       * This block of code is the original way of loading DICOM into dicom-microscopy-viewer
       * as in their documentation.
       * But we have the metadata already loaded by our loaders.
       * As the metadata for microscopy DIOM files tends to be big and we don't
       * want to double load it, below we have the mechanism to reconstruct the
       * DICOM JSON structure (denaturalized) from naturalized metadata.
       * (NOTE: Our loaders cache only naturalized metadata, not the denaturalized.)
       */
      // {
      //   const retrieveOptions = {
      //     studyInstanceUID: metadata[0].StudyInstanceUID,
      //     seriesInstanceUID: metadata[0].SeriesInstanceUID,
      //   };
      //   metadata = await client.retrieveSeriesMetadata(retrieveOptions);
      //   // Parse, format, and filter metadata
      //   metadata.forEach(m => {
      //     if (
      //       volumeImages.length > 0 &&
      //       m['00200052'].Value[0] != volumeImages[0].FrameOfReferenceUID
      //     ) {
      //       console.warn(
      //         'Expected FrameOfReferenceUID of difference instances within a series to be the same, found multiple different values',
      //         m['00200052'].Value[0]
      //       );
      //       m['00200052'].Value[0] = volumeImages[0].FrameOfReferenceUID;
      //     }
      //     NOTE: depending on different data source, image.ImageType sometimes
      //     is a string, not a string array.
      //     m['00080008'] = transformImageTypeUnnaturalized(m['00080008']);

      //     const image = new metadataUtils.VLWholeSlideMicroscopyImage({
      //       metadata: m,
      //     });
      //     const imageFlavor = image.ImageType[2];
      //     if (imageFlavor === 'VOLUME' || imageFlavor === 'THUMBNAIL') {
      //       volumeImages.push(image);
      //     }
      //   });
      // }

      metadata.forEach(m => {
        // NOTE: depending on different data source, image.ImageType sometimes
        //    is a string, not a string array.
        m.ImageType = typeof m.ImageType === 'string' ? m.ImageType.split('\\') : m.ImageType;
        const inst = cleanDenaturalizedDataset(dcmjs_es["default"].data.DicomMetaDictionary.denaturalizeDataset(m), {
          StudyInstanceUID: m.StudyInstanceUID,
          SeriesInstanceUID: m.SeriesInstanceUID,
          dataSourceConfig: this.props.dataSource.getConfig()
        });
        if (!inst['00480105']) {
          // Optical Path Sequence, no OpticalPathIdentifier?
          // NOTE: this is actually a not-well formatted DICOM VL Whole Slide Microscopy Image.
          inst['00480105'] = {
            vr: 'SQ',
            Value: [{
              '00480106': {
                vr: 'SH',
                Value: ['1']
              }
            }]
          };
        }
        const image = new metadataUtils.VLWholeSlideMicroscopyImage({
          metadata: inst
        });
        const imageFlavor = image.ImageType[2];
        if (imageFlavor === 'VOLUME' || imageFlavor === 'THUMBNAIL') {
          volumeImages.push(image);
        }
      });

      // format metadata for microscopy-viewer
      const options = {
        client,
        metadata: volumeImages,
        retrieveRendered: false,
        controls: ['overview', 'position']
      };
      this.viewer = new microscopyViewer(options);
      if (this.overlayElement && this.overlayElement.current && this.viewer.addViewportOverlay) {
        this.viewer.addViewportOverlay({
          element: this.overlayElement.current,
          coordinates: [0, 0],
          // TODO: dicom-microscopy-viewer documentation says this can be false to be automatically, but it is not.
          navigate: true,
          className: 'OpenLayersOverlay'
        });
      }
      this.viewer.render({
        container
      });
      const {
        StudyInstanceUID,
        SeriesInstanceUID
      } = displaySet;
      this.managedViewer = this.microscopyService.addViewer(this.viewer, this.props.viewportId, container, StudyInstanceUID, SeriesInstanceUID);
      this.managedViewer.addContextMenuCallback(event => {
        // TODO: refactor this after Bill's changes on ContextMenu feature get merged
        // const roiAnnotationNearBy = this.getNearbyROI(event);
      });
    };
    this.microscopyService.clearAnnotations();
    let smDisplaySet = displaySet;
    if (displaySet.Modality === 'SR') {
      // for SR displaySet, let's load the actual image displaySet
      smDisplaySet = displaySet.getSourceDisplaySet();
    }
    console.log('Loading viewer metadata', smDisplaySet);
    await loadViewer(smDisplaySet.others);
    if (displaySet.Modality === 'SR') {
      displaySet.load(smDisplaySet);
    }
  }
  componentDidMount() {
    const {
      displaySets,
      viewportOptions
    } = this.props;
    const {
      viewportId
    } = viewportOptions;
    // Todo-rename: this is always getting the 0
    const displaySet = displaySets[0];
    this.installOpenLayersRenderer(this.container.current, displaySet).then(() => {
      this.setState({
        isLoaded: true
      });
    });
  }
  componentDidUpdate(prevProps, prevState, snapshot) {
    if (this.managedViewer && prevProps.displaySets !== this.props.displaySets) {
      const {
        displaySets
      } = this.props;
      const displaySet = displaySets[0];
      this.microscopyService.clearAnnotations();

      // loading SR
      if (displaySet.Modality === 'SR') {
        const referencedDisplaySet = displaySet.getSourceDisplaySet();
        displaySet.load(referencedDisplaySet);
      }
    }
  }
  componentWillUnmount() {
    this.microscopyService.removeViewer(this.viewer);
  }
  render() {
    const style = {
      width: '100%',
      height: '100%'
    };
    const displaySet = this.props.displaySets[0];
    const firstInstance = displaySet.firstInstance || displaySet.instance;
    return /*#__PURE__*/react.createElement("div", {
      className: 'DicomMicroscopyViewer',
      style: style,
      onClick: this.setViewportActiveHandler
    }, /*#__PURE__*/react.createElement("div", {
      style: {
        ...style,
        display: 'none'
      }
    }, /*#__PURE__*/react.createElement("div", {
      style: {
        ...style
      },
      ref: this.overlayElement
    }, /*#__PURE__*/react.createElement("div", {
      style: {
        position: 'relative',
        height: '100%',
        width: '100%'
      }
    }, displaySet && firstInstance.imageId && /*#__PURE__*/react.createElement(ViewportOverlay, {
      displaySet: displaySet,
      instance: displaySet.instance,
      metadata: displaySet.metadata
    })))), index_esm/* default */.ZP && /*#__PURE__*/react.createElement(index_esm/* default */.ZP, {
      handleWidth: true,
      handleHeight: true,
      onResize: this.onWindowResize
    }), this.state.error ? /*#__PURE__*/react.createElement("h2", null, JSON.stringify(this.state.error)) : /*#__PURE__*/react.createElement("div", {
      style: style,
      ref: this.container
    }), this.state.isLoaded ? null : /*#__PURE__*/react.createElement(src/* LoadingIndicatorProgress */.LE, {
      className: 'h-full w-full bg-black'
    }));
  }
}
DicomMicroscopyViewport.propTypes = {
  viewportData: (prop_types_default()).object,
  activeViewportId: (prop_types_default()).string,
  setViewportActive: (prop_types_default()).func,
  // props from OHIF Viewport Grid
  displaySets: (prop_types_default()).array,
  viewportId: (prop_types_default()).string,
  viewportLabel: (prop_types_default()).string,
  dataSource: (prop_types_default()).object,
  viewportOptions: (prop_types_default()).object,
  displaySetOptions: (prop_types_default()).array,
  // other props from wrapping component
  servicesManager: (prop_types_default()).object,
  extensionManager: (prop_types_default()).object,
  commandsManager: (prop_types_default()).object
};
/* harmony default export */ const src_DicomMicroscopyViewport = (DicomMicroscopyViewport);

/***/ })

}]);