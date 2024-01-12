(self["webpackChunk"] = self["webpackChunk"] || []).push([[236],{

/***/ 80965:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
// ESM COMPAT FLAG
__webpack_require__.r(__webpack_exports__);

// EXPORTS
__webpack_require__.d(__webpack_exports__, {
  "default": () => (/* binding */ dicom_microscopy_src)
});

;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/package.json
const package_namespaceObject = JSON.parse('{"u2":"@ohif/extension-dicom-microscopy"}');
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/id.js

const id = package_namespaceObject.u2;

// EXTERNAL MODULE: ../../../node_modules/react/index.js
var react = __webpack_require__(43001);
// EXTERNAL MODULE: ../../ui/src/index.js + 485 modules
var src = __webpack_require__(71783);
// EXTERNAL MODULE: ../../core/src/index.ts + 65 modules
var core_src = __webpack_require__(71771);
// EXTERNAL MODULE: ../../../node_modules/react-i18next/dist/es/index.js + 15 modules
var es = __webpack_require__(69190);
// EXTERNAL MODULE: ../../../node_modules/mathjs/lib/esm/index.js + 982 modules
var esm = __webpack_require__(55220);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/coordinateFormatScoord3d2Geometry.js


// TODO -> This is pulled out of some internal logic from Dicom Microscopy Viewer,
// We should likely just expose this there.

function coordinateFormatScoord3d2Geometry(coordinates, pyramid) {
  let transform = false;
  if (!Array.isArray(coordinates[0])) {
    coordinates = [coordinates];
    transform = true;
  }
  const metadata = pyramid[pyramid.length - 1];
  const orientation = metadata.ImageOrientationSlide;
  const spacing = _getPixelSpacing(metadata);
  const origin = metadata.TotalPixelMatrixOriginSequence[0];
  const offset = [Number(origin.XOffsetInSlideCoordinateSystem), Number(origin.YOffsetInSlideCoordinateSystem)];
  coordinates = coordinates.map(c => {
    const slideCoord = [c[0], c[1]];
    const pixelCoord = mapSlideCoord2PixelCoord({
      offset,
      orientation,
      spacing,
      point: slideCoord
    });
    return [pixelCoord[0], -(pixelCoord[1] + 1), 0];
  });
  if (transform) {
    return coordinates[0];
  }
  return coordinates;
}
function _getPixelSpacing(metadata) {
  if (metadata.PixelSpacing) {
    return metadata.PixelSpacing;
  }
  const functionalGroup = metadata.SharedFunctionalGroupsSequence[0];
  const pixelMeasures = functionalGroup.PixelMeasuresSequence[0];
  return pixelMeasures.PixelSpacing;
}
function mapSlideCoord2PixelCoord(options) {
  // X and Y Offset in Slide Coordinate System
  if (!('offset' in options)) {
    throw new Error('Option "offset" is required.');
  }
  if (!Array.isArray(options.offset)) {
    throw new Error('Option "offset" must be an array.');
  }
  if (options.offset.length !== 2) {
    throw new Error('Option "offset" must be an array with 2 elements.');
  }
  const offset = options.offset;

  // Image Orientation Slide with direction cosines for Row and Column direction
  if (!('orientation' in options)) {
    throw new Error('Option "orientation" is required.');
  }
  if (!Array.isArray(options.orientation)) {
    throw new Error('Option "orientation" must be an array.');
  }
  if (options.orientation.length !== 6) {
    throw new Error('Option "orientation" must be an array with 6 elements.');
  }
  const orientation = options.orientation;

  // Pixel Spacing along the Row and Column direction
  if (!('spacing' in options)) {
    throw new Error('Option "spacing" is required.');
  }
  if (!Array.isArray(options.spacing)) {
    throw new Error('Option "spacing" must be an array.');
  }
  if (options.spacing.length !== 2) {
    throw new Error('Option "spacing" must be an array with 2 elements.');
  }
  const spacing = options.spacing;

  // X and Y coordinate in the Slide Coordinate System
  if (!('point' in options)) {
    throw new Error('Option "point" is required.');
  }
  if (!Array.isArray(options.point)) {
    throw new Error('Option "point" must be an array.');
  }
  if (options.point.length !== 2) {
    throw new Error('Option "point" must be an array with 2 elements.');
  }
  const point = options.point;
  const m = [[orientation[0] * spacing[1], orientation[3] * spacing[0], offset[0]], [orientation[1] * spacing[1], orientation[4] * spacing[0], offset[1]], [0, 0, 1]];
  const mInverted = (0,esm/* inv */.JBn)(m);
  const vSlide = [[point[0]], [point[1]], [1]];
  const vImage = (0,esm/* multiply */.JpY)(mInverted, vSlide);
  const row = Number(vImage[1][0].toFixed(4));
  const col = Number(vImage[0][0].toFixed(4));
  return [col, row];
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/styles.js
const defaultFill = {
  color: 'rgba(255,255,255,0.4)'
};
const emptyFill = {
  color: 'rgba(255,255,255,0.0)'
};
const defaultStroke = {
  color: 'rgb(0,255,0)',
  width: 1.5
};
const activeStroke = {
  color: 'rgb(255,255,0)',
  width: 1.5
};
const defaultStyle = {
  image: {
    circle: {
      fill: defaultFill,
      stroke: activeStroke,
      radius: 5
    }
  },
  fill: defaultFill,
  stroke: activeStroke
};
const emptyStyle = {
  image: {
    circle: {
      fill: emptyFill,
      stroke: defaultStroke,
      radius: 5
    }
  },
  fill: emptyFill,
  stroke: defaultStroke
};
const styles = {
  active: defaultStyle,
  default: emptyStyle
};
/* harmony default export */ const utils_styles = (styles);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/tools/viewerManager.js




// Events from the third-party viewer
const ApiEvents = {
  /** Triggered when a ROI was added. */
  ROI_ADDED: 'dicommicroscopyviewer_roi_added',
  /** Triggered when a ROI was modified. */
  ROI_MODIFIED: 'dicommicroscopyviewer_roi_modified',
  /** Triggered when a ROI was removed. */
  ROI_REMOVED: 'dicommicroscopyviewer_roi_removed',
  /** Triggered when a ROI was drawn. */
  ROI_DRAWN: `dicommicroscopyviewer_roi_drawn`,
  /** Triggered when a ROI was selected. */
  ROI_SELECTED: `dicommicroscopyviewer_roi_selected`,
  /** Triggered when a viewport move has started. */
  MOVE_STARTED: `dicommicroscopyviewer_move_started`,
  /** Triggered when a viewport move has ended. */
  MOVE_ENDED: `dicommicroscopyviewer_move_ended`,
  /** Triggered when a loading of data has started. */
  LOADING_STARTED: `dicommicroscopyviewer_loading_started`,
  /** Triggered when a loading of data has ended. */
  LOADING_ENDED: `dicommicroscopyviewer_loading_ended`,
  /** Triggered when an error occurs during loading of data. */
  LOADING_ERROR: `dicommicroscopyviewer_loading_error`,
  /* Triggered when the loading of an image tile has started. */
  FRAME_LOADING_STARTED: `dicommicroscopyviewer_frame_loading_started`,
  /* Triggered when the loading of an image tile has ended. */
  FRAME_LOADING_ENDED: `dicommicroscopyviewer_frame_loading_ended`,
  /* Triggered when the error occurs during loading of an image tile. */
  FRAME_LOADING_ERROR: `dicommicroscopyviewer_frame_loading_ended`
};
const EVENTS = {
  ADDED: 'added',
  MODIFIED: 'modified',
  REMOVED: 'removed',
  UPDATED: 'updated',
  SELECTED: 'selected'
};

/**
 * ViewerManager encapsulates the complexity of the third-party viewer and
 * expose only the features/behaviors that are relevant to the application
 */
class ViewerManager extends core_src/* PubSubService */.hC {
  constructor(viewer, viewportId, container, studyInstanceUID, seriesInstanceUID) {
    super(EVENTS);
    this.viewer = viewer;
    this.viewportId = viewportId;
    this.container = container;
    this.studyInstanceUID = studyInstanceUID;
    this.seriesInstanceUID = seriesInstanceUID;
    this.onRoiAdded = this.roiAddedHandler.bind(this);
    this.onRoiModified = this.roiModifiedHandler.bind(this);
    this.onRoiRemoved = this.roiRemovedHandler.bind(this);
    this.onRoiSelected = this.roiSelectedHandler.bind(this);
    this.contextMenuCallback = () => {};

    // init symbols
    const symbols = Object.getOwnPropertySymbols(this.viewer);
    this._drawingSource = symbols.find(p => p.description === 'drawingSource');
    this._pyramid = symbols.find(p => p.description === 'pyramid');
    this._map = symbols.find(p => p.description === 'map');
    this._affine = symbols.find(p => p.description === 'affine');
    this.registerEvents();
    this.activateDefaultInteractions();
  }
  addContextMenuCallback(callback) {
    this.contextMenuCallback = callback;
  }

  /**
   * Destroys this managed viewer instance, clearing all the event handlers
   */
  destroy() {
    this.unregisterEvents();
  }

  /**
   * This is to overrides the _broadcastEvent method of PubSubService and always
   * send the ROI graphic object and this managed viewer instance.
   * Due to the way that PubSubService is written, the same name override of the
   * function doesn't work.
   *
   * @param {String} key key Subscription key
   * @param {Object} roiGraphic ROI graphic object created by the third-party API
   */
  publish(key, roiGraphic) {
    this._broadcastEvent(key, {
      roiGraphic,
      managedViewer: this
    });
  }

  /**
   * Registers all the relevant event handlers for the third-party API
   */
  registerEvents() {
    this.container.addEventListener(ApiEvents.ROI_ADDED, this.onRoiAdded);
    this.container.addEventListener(ApiEvents.ROI_MODIFIED, this.onRoiModified);
    this.container.addEventListener(ApiEvents.ROI_REMOVED, this.onRoiRemoved);
    this.container.addEventListener(ApiEvents.ROI_SELECTED, this.onRoiSelected);
  }

  /**
   * Clears all the relevant event handlers for the third-party API
   */
  unregisterEvents() {
    this.container.removeEventListener(ApiEvents.ROI_ADDED, this.onRoiAdded);
    this.container.removeEventListener(ApiEvents.ROI_MODIFIED, this.onRoiModified);
    this.container.removeEventListener(ApiEvents.ROI_REMOVED, this.onRoiRemoved);
    this.container.removeEventListener(ApiEvents.ROI_SELECTED, this.onRoiSelected);
  }

  /**
   * Handles the ROI_ADDED event triggered by the third-party API
   *
   * @param {Event} event Event triggered by the third-party API
   */
  roiAddedHandler(event) {
    const roiGraphic = event.detail.payload;
    this.publish(EVENTS.ADDED, roiGraphic);
    this.publish(EVENTS.UPDATED, roiGraphic);
  }

  /**
   * Handles the ROI_MODIFIED event triggered by the third-party API
   *
   * @param {Event} event Event triggered by the third-party API
   */
  roiModifiedHandler(event) {
    const roiGraphic = event.detail.payload;
    this.publish(EVENTS.MODIFIED, roiGraphic);
    this.publish(EVENTS.UPDATED, roiGraphic);
  }

  /**
   * Handles the ROI_REMOVED event triggered by the third-party API
   *
   * @param {Event} event Event triggered by the third-party API
   */
  roiRemovedHandler(event) {
    const roiGraphic = event.detail.payload;
    this.publish(EVENTS.REMOVED, roiGraphic);
    this.publish(EVENTS.UPDATED, roiGraphic);
  }

  /**
   * Handles the ROI_SELECTED event triggered by the third-party API
   *
   * @param {Event} event Event triggered by the third-party API
   */
  roiSelectedHandler(event) {
    const roiGraphic = event.detail.payload;
    this.publish(EVENTS.SELECTED, roiGraphic);
  }

  /**
   * Run the given callback operation without triggering any events for this
   * instance, so subscribers will not be affected
   *
   * @param {Function} callback Callback that will run sinlently
   */
  runSilently(callback) {
    this.unregisterEvents();
    callback();
    this.registerEvents();
  }

  /**
   * Removes all the ROI graphics from the third-party API
   */
  clearRoiGraphics() {
    this.runSilently(() => this.viewer.removeAllROIs());
  }
  showROIs() {
    this.viewer.showROIs();
  }
  hideROIs() {
    this.viewer.hideROIs();
  }

  /**
   * Adds the given ROI graphic into the third-party API
   *
   * @param {Object} roiGraphic ROI graphic object to be added
   */
  addRoiGraphic(roiGraphic) {
    this.runSilently(() => this.viewer.addROI(roiGraphic, utils_styles["default"]));
  }

  /**
   * Adds the given ROI graphic into the third-party API, and also add a label.
   * Used for importing from SR.
   *
   * @param {Object} roiGraphic ROI graphic object to be added.
   * @param {String} label The label of the annotation.
   */
  addRoiGraphicWithLabel(roiGraphic, label) {
    // NOTE: Dicom Microscopy Viewer will override styles for "Text" evaluations
    // to hide all other geometries, we are not going to use its label.
    // if (label) {
    //   if (!roiGraphic.properties) roiGraphic.properties = {};
    //   roiGraphic.properties.label = label;
    // }
    this.runSilently(() => this.viewer.addROI(roiGraphic, utils_styles["default"]));
    this._broadcastEvent(EVENTS.ADDED, {
      roiGraphic,
      managedViewer: this,
      label
    });
  }

  /**
   * Sets ROI style
   *
   * @param {String} uid ROI graphic UID to be styled
   * @param {object} styleOptions - Style options
   * @param {object} styleOptions.stroke - Style options for the outline of the geometry
   * @param {number[]} styleOptions.stroke.color - RGBA color of the outline
   * @param {number} styleOptions.stroke.width - Width of the outline
   * @param {object} styleOptions.fill - Style options for body the geometry
   * @param {number[]} styleOptions.fill.color - RGBA color of the body
   * @param {object} styleOptions.image - Style options for image
   */
  setROIStyle(uid, styleOptions) {
    this.viewer.setROIStyle(uid, styleOptions);
  }

  /**
   * Removes the ROI graphic with the given UID from the third-party API
   *
   * @param {String} uid ROI graphic UID to be removed
   */
  removeRoiGraphic(uid) {
    this.viewer.removeROI(uid);
  }

  /**
   * Update properties of regions of interest.
   *
   * @param {object} roi - ROI to be updated
   * @param {string} roi.uid - Unique identifier of the region of interest
   * @param {object} roi.properties - ROI properties
   * @returns {void}
   */
  updateROIProperties(_ref) {
    let {
      uid,
      properties
    } = _ref;
    this.viewer.updateROI({
      uid,
      properties
    });
  }

  /**
   * Toggles overview map
   *
   * @returns {void}
   */
  toggleOverviewMap() {
    this.viewer.toggleOverviewMap();
  }

  /**
   * Activates the viewer default interactions
   * @returns {void}
   */
  activateDefaultInteractions() {
    /** Disable browser's native context menu inside the canvas */
    document.querySelector('.DicomMicroscopyViewer').addEventListener('contextmenu', event => {
      event.preventDefault();
      // comment out when context menu for microscopy is enabled
      // if (typeof this.contextMenuCallback === 'function') {
      //   this.contextMenuCallback(event);
      // }
    }, false);
    const defaultInteractions = [['dragPan', {
      bindings: {
        mouseButtons: ['middle']
      }
    }], ['dragZoom', {
      bindings: {
        mouseButtons: ['right']
      }
    }], ['modify', {}]];
    this.activateInteractions(defaultInteractions);
  }

  /**
   * Activates interactions
   * @param {Array} interactions Interactions to be activated
   * @returns {void}
   */
  activateInteractions(interactions) {
    const interactionsMap = {
      draw: activate => activate ? 'activateDrawInteraction' : 'deactivateDrawInteraction',
      modify: activate => activate ? 'activateModifyInteraction' : 'deactivateModifyInteraction',
      translate: activate => activate ? 'activateTranslateInteraction' : 'deactivateTranslateInteraction',
      snap: activate => activate ? 'activateSnapInteraction' : 'deactivateSnapInteraction',
      dragPan: activate => activate ? 'activateDragPanInteraction' : 'deactivateDragPanInteraction',
      dragZoom: activate => activate ? 'activateDragZoomInteraction' : 'deactivateDragZoomInteraction',
      select: activate => activate ? 'activateSelectInteraction' : 'deactivateSelectInteraction'
    };
    const availableInteractionsName = Object.keys(interactionsMap);
    availableInteractionsName.forEach(availableInteractionName => {
      const interaction = interactions.find(interaction => interaction[0] === availableInteractionName);
      if (!interaction) {
        const deactivateInteractionMethod = interactionsMap[availableInteractionName](false);
        this.viewer[deactivateInteractionMethod]();
      } else {
        const [name, config] = interaction;
        const activateInteractionMethod = interactionsMap[name](true);
        this.viewer[activateInteractionMethod](config);
      }
    });
  }

  /**
   * Accesses the internals of third-party API and returns the OpenLayers Map
   *
   * @returns {Object} OpenLayers Map component instance
   */
  _getMapView() {
    const map = this._getMap();
    return map.getView();
  }
  _getMap() {
    const symbols = Object.getOwnPropertySymbols(this.viewer);
    const _map = symbols.find(s => String(s) === 'Symbol(map)');
    window['map'] = this.viewer[_map];
    return this.viewer[_map];
  }

  /**
   * Returns the current state for the OpenLayers View
   *
   * @returns {Object} Current view state
   */
  getViewState() {
    const view = this._getMapView();
    return {
      center: view.getCenter(),
      resolution: view.getResolution(),
      zoom: view.getZoom()
    };
  }

  /**
   * Sets the current state for the OpenLayers View
   *
   * @param {Object} viewState View state to be applied
   */
  setViewState(viewState) {
    const view = this._getMapView();
    view.setZoom(viewState.zoom);
    view.setResolution(viewState.resolution);
    view.setCenter(viewState.center);
  }
  setViewStateByExtent(roiAnnotation) {
    const coordinates = roiAnnotation.getCoordinates();
    if (Array.isArray(coordinates[0]) && !coordinates[2]) {
      this._jumpToPolyline(coordinates);
    } else if (Array.isArray(coordinates[0])) {
      this._jumpToPolygonOrEllipse(coordinates);
    } else {
      this._jumpToPoint(coordinates);
    }
  }
  _jumpToPoint(coord) {
    const pyramid = this.viewer[this._pyramid].metadata;
    const mappedCoord = coordinateFormatScoord3d2Geometry(coord, pyramid);
    const view = this._getMapView();
    view.setCenter(mappedCoord);
  }
  _jumpToPolyline(coord) {
    const pyramid = this.viewer[this._pyramid].metadata;
    const mappedCoord = coordinateFormatScoord3d2Geometry(coord, pyramid);
    const view = this._getMapView();
    const x = mappedCoord[0];
    const y = mappedCoord[1];
    const xab = (x[0] + y[0]) / 2;
    const yab = (x[1] + y[1]) / 2;
    const midpoint = [xab, yab];
    view.setCenter(midpoint);
  }
  _jumpToPolygonOrEllipse(coordinates) {
    const pyramid = this.viewer[this._pyramid].metadata;
    let minX = Infinity;
    let maxX = -Infinity;
    let minY = Infinity;
    let maxY = -Infinity;
    coordinates.forEach(coord => {
      let mappedCoord = coordinateFormatScoord3d2Geometry(coord, pyramid);
      const [x, y] = mappedCoord;
      if (x < minX) {
        minX = x;
      } else if (x > maxX) {
        maxX = x;
      }
      if (y < minY) {
        minY = y;
      } else if (y > maxY) {
        maxY = y;
      }
    });
    const width = maxX - minX;
    const height = maxY - minY;
    minX -= 0.5 * width;
    maxX += 0.5 * width;
    minY -= 0.5 * height;
    maxY += 0.5 * height;
    const map = this._getMap();
    map.getView().fit([minX, minY, maxX, maxY], map.getSize());
  }
}

/* harmony default export */ const viewerManager = (ViewerManager);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/areaOfPolygon.js
function areaOfPolygon(coordinates) {
  // Shoelace algorithm.
  const n = coordinates.length;
  let area = 0.0;
  let j = n - 1;
  for (let i = 0; i < n; i++) {
    area += (coordinates[j][0] + coordinates[i][0]) * (coordinates[j][1] - coordinates[i][1]);
    j = i; // j is previous vertex to i
  }

  // Return absolute value of half the sum
  // (The value is halved as we are summing up triangles, not rectangles).
  return Math.abs(area / 2.0);
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/RoiAnnotation.js


const RoiAnnotation_EVENTS = {
  LABEL_UPDATED: 'labelUpdated',
  GRAPHIC_UPDATED: 'graphicUpdated',
  VIEW_UPDATED: 'viewUpdated',
  REMOVED: 'removed'
};

/**
 * Represents a single annotation for the Microscopy Viewer
 */
class RoiAnnotation extends core_src/* PubSubService */.hC {
  constructor(roiGraphic, studyInstanceUID, seriesInstanceUID) {
    let label = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : '';
    let viewState = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : null;
    super(RoiAnnotation_EVENTS);
    this.uid = roiGraphic.uid;
    this.roiGraphic = roiGraphic;
    this.studyInstanceUID = studyInstanceUID;
    this.seriesInstanceUID = seriesInstanceUID;
    this.label = label;
    this.viewState = viewState;
    this.setMeasurements(roiGraphic);
  }
  getScoord3d() {
    const roiGraphic = this.roiGraphic;
    const roiGraphicSymbols = Object.getOwnPropertySymbols(roiGraphic);
    const _scoord3d = roiGraphicSymbols.find(s => String(s) === 'Symbol(scoord3d)');
    return roiGraphic[_scoord3d];
  }
  getCoordinates() {
    const scoord3d = this.getScoord3d();
    const scoord3dSymbols = Object.getOwnPropertySymbols(scoord3d);
    const _coordinates = scoord3dSymbols.find(s => String(s) === 'Symbol(coordinates)');
    const coordinates = scoord3d[_coordinates];
    return coordinates;
  }

  /**
   * When called will trigger the REMOVED event
   */
  destroy() {
    this._broadcastEvent(RoiAnnotation_EVENTS.REMOVED, this);
  }

  /**
   * Updates the ROI graphic for the annotation and triggers the GRAPHIC_UPDATED
   * event
   *
   * @param {Object} roiGraphic
   */
  setRoiGraphic(roiGraphic) {
    this.roiGraphic = roiGraphic;
    this.setMeasurements();
    this._broadcastEvent(RoiAnnotation_EVENTS.GRAPHIC_UPDATED, this);
  }

  /**
   * Update ROI measurement values based on its scoord3d coordinates.
   *
   * @returns {void}
   */
  setMeasurements() {
    const type = this.roiGraphic.scoord3d.graphicType;
    const coordinates = this.roiGraphic.scoord3d.graphicData;
    switch (type) {
      case 'ELLIPSE':
        // This is a circle so only need one side
        const point1 = coordinates[0];
        const point2 = coordinates[1];
        let xLength2 = point2[0] - point1[0];
        let yLength2 = point2[1] - point1[1];
        xLength2 *= xLength2;
        yLength2 *= yLength2;
        const length = Math.sqrt(xLength2 + yLength2);
        const radius = length / 2;
        const areaEllipse = Math.PI * radius * radius;
        this._area = areaEllipse;
        this._length = undefined;
        break;
      case 'POLYGON':
        const areaPolygon = areaOfPolygon(coordinates);
        this._area = areaPolygon;
        this._length = undefined;
        break;
      case 'POINT':
        this._area = undefined;
        this._length = undefined;
        break;
      case 'POLYLINE':
        let len = 0;
        for (let i = 1; i < coordinates.length; i++) {
          const p1 = coordinates[i - 1];
          const p2 = coordinates[i];
          let xLen = p2[0] - p1[0];
          let yLen = p2[1] - p1[1];
          xLen *= xLen;
          yLen *= yLen;
          len += Math.sqrt(xLen + yLen);
        }
        this._area = undefined;
        this._length = len;
        break;
    }
  }

  /**
   * Update the OpenLayer Map's view state for the annotation and triggers the
   * VIEW_UPDATED event
   *
   * @param {Object} viewState The new view state for the annotation
   */
  setViewState(viewState) {
    this.viewState = viewState;
    this._broadcastEvent(RoiAnnotation_EVENTS.VIEW_UPDATED, this);
  }

  /**
   * Update the label for the annotation and triggers the LABEL_UPDATED event
   *
   * @param {String} label New label for the annotation
   */
  setLabel(label, finding) {
    this.label = label || finding && finding.CodeMeaning;
    this.finding = finding || {
      CodingSchemeDesignator: '@ohif/extension-dicom-microscopy',
      CodeValue: label,
      CodeMeaning: label
    };
    this._broadcastEvent(RoiAnnotation_EVENTS.LABEL_UPDATED, this);
  }

  /**
   * Returns the geometry type of the annotation concatenated with the label
   * defined for the annotation.
   * Difference with getDetailedLabel() is that this will return empty string for empty
   * label.
   *
   * @returns {String} Text with geometry type and label
   */
  getLabel() {
    const label = this.label ? `${this.label}` : '';
    return label;
  }

  /**
   * Returns the geometry type of the annotation concatenated with the label
   * defined for the annotation
   *
   * @returns {String} Text with geometry type and label
   */
  getDetailedLabel() {
    const label = this.label ? `${this.label}` : '(empty)';
    return label;
  }
  getLength() {
    return this._length;
  }
  getArea() {
    return this._area;
  }
}

/* harmony default export */ const utils_RoiAnnotation = (RoiAnnotation);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/services/MicroscopyService.ts




const MicroscopyService_EVENTS = {
  ANNOTATION_UPDATED: 'annotationUpdated',
  ANNOTATION_SELECTED: 'annotationSelected',
  ANNOTATION_REMOVED: 'annotationRemoved',
  RELABEL: 'relabel',
  DELETE: 'delete'
};

/**
 * MicroscopyService is responsible to manage multiple third-party API's
 * microscopy viewers expose methods to manage the interaction with these
 * viewers and handle their ROI graphics to create, remove and modify the
 * ROI annotations relevant to the application
 */
class MicroscopyService extends core_src/* PubSubService */.hC {
  constructor(serviceManager) {
    super(MicroscopyService_EVENTS);
    this.serviceManager = void 0;
    this.managedViewers = new Set();
    this.roiUids = new Set();
    this.annotations = {};
    this.selectedAnnotation = null;
    this.pendingFocus = false;
    this.serviceManager = serviceManager;
    this._onRoiAdded = this._onRoiAdded.bind(this);
    this._onRoiModified = this._onRoiModified.bind(this);
    this._onRoiRemoved = this._onRoiRemoved.bind(this);
    this._onRoiUpdated = this._onRoiUpdated.bind(this);
    this._onRoiSelected = this._onRoiSelected.bind(this);
    this.isROIsVisible = true;
  }

  /**
   * Clears all the annotations and managed viewers, setting the manager state
   * to its initial state
   */
  clear() {
    this.managedViewers.forEach(managedViewer => managedViewer.destroy());
    this.managedViewers.clear();
    for (const key in this.annotations) {
      delete this.annotations[key];
    }
    this.roiUids.clear();
    this.selectedAnnotation = null;
    this.pendingFocus = false;
  }
  clearAnnotations() {
    Object.keys(this.annotations).forEach(uid => {
      this.removeAnnotation(this.annotations[uid]);
    });
  }

  /**
   * Observes when a ROI graphic is added, creating the correspondent annotation
   * with the current graphic and view state.
   * Creates a subscription for label updating for the created annotation and
   * publishes an ANNOTATION_UPDATED event when it happens.
   * Also triggers the relabel process after the graphic is placed.
   *
   * @param {Object} data The published data
   * @param {Object} data.roiGraphic The added ROI graphic object
   * @param {ViewerManager} data.managedViewer The origin viewer for the event
   */
  _onRoiAdded(data) {
    const {
      roiGraphic,
      managedViewer,
      label
    } = data;
    const {
      studyInstanceUID,
      seriesInstanceUID
    } = managedViewer;
    const viewState = managedViewer.getViewState();
    const roiAnnotation = new utils_RoiAnnotation(roiGraphic, studyInstanceUID, seriesInstanceUID, '', viewState);
    this.roiUids.add(roiGraphic.uid);
    this.annotations[roiGraphic.uid] = roiAnnotation;
    roiAnnotation.subscribe(RoiAnnotation_EVENTS.LABEL_UPDATED, () => {
      this._broadcastEvent(MicroscopyService_EVENTS.ANNOTATION_UPDATED, roiAnnotation);
    });
    if (label !== undefined) {
      roiAnnotation.setLabel(label);
    } else {
      const onRelabel = item => managedViewer.updateROIProperties({
        uid: roiGraphic.uid,
        properties: {
          label: item.label,
          finding: item.finding
        }
      });
      this.triggerRelabel(roiAnnotation, true, onRelabel);
    }
  }

  /**
   * Observes when a ROI graphic is modified, updating the correspondent
   * annotation with the current graphic and view state.
   *
   * @param {Object} data The published data
   * @param {Object} data.roiGraphic The modified ROI graphic object
   */
  _onRoiModified(data) {
    const {
      roiGraphic,
      managedViewer
    } = data;
    const roiAnnotation = this.getAnnotation(roiGraphic.uid);
    if (!roiAnnotation) {
      return;
    }
    roiAnnotation.setRoiGraphic(roiGraphic);
    roiAnnotation.setViewState(managedViewer.getViewState());
  }

  /**
   * Observes when a ROI graphic is removed, reflecting the removal in the
   * annotations' state.
   *
   * @param {Object} data The published data
   * @param {Object} data.roiGraphic The removed ROI graphic object
   */
  _onRoiRemoved(data) {
    const {
      roiGraphic
    } = data;
    this.roiUids.delete(roiGraphic.uid);
    this.annotations[roiGraphic.uid].destroy();
    delete this.annotations[roiGraphic.uid];
    this._broadcastEvent(MicroscopyService_EVENTS.ANNOTATION_REMOVED, roiGraphic);
  }

  /**
   * Observes any changes on ROI graphics and synchronize all the managed
   * viewers to reflect those changes.
   * Also publishes an ANNOTATION_UPDATED event to notify the subscribers.
   *
   * @param {Object} data The published data
   * @param {Object} data.roiGraphic The added ROI graphic object
   * @param {ViewerManager} data.managedViewer The origin viewer for the event
   */
  _onRoiUpdated(data) {
    const {
      roiGraphic,
      managedViewer
    } = data;
    this.synchronizeViewers(managedViewer);
    this._broadcastEvent(MicroscopyService_EVENTS.ANNOTATION_UPDATED, this.getAnnotation(roiGraphic.uid));
  }

  /**
   * Observes when an ROI is selected.
   * Also publishes an ANNOTATION_SELECTED event to notify the subscribers.
   *
   * @param {Object} data The published data
   * @param {Object} data.roiGraphic The added ROI graphic object
   * @param {ViewerManager} data.managedViewer The origin viewer for the event
   */
  _onRoiSelected(data) {
    const {
      roiGraphic
    } = data;
    const selectedAnnotation = this.getAnnotation(roiGraphic.uid);
    if (selectedAnnotation && selectedAnnotation !== this.getSelectedAnnotation()) {
      if (this.selectedAnnotation) {
        this.clearSelection();
      }
      this.selectedAnnotation = selectedAnnotation;
      this._broadcastEvent(MicroscopyService_EVENTS.ANNOTATION_SELECTED, selectedAnnotation);
    }
  }

  /**
   * Creates the subscriptions for the managed viewer being added
   *
   * @param {ViewerManager} managedViewer The viewer being added
   */
  _addManagedViewerSubscriptions(managedViewer) {
    managedViewer._roiAddedSubscription = managedViewer.subscribe(EVENTS.ADDED, this._onRoiAdded);
    managedViewer._roiModifiedSubscription = managedViewer.subscribe(EVENTS.MODIFIED, this._onRoiModified);
    managedViewer._roiRemovedSubscription = managedViewer.subscribe(EVENTS.REMOVED, this._onRoiRemoved);
    managedViewer._roiUpdatedSubscription = managedViewer.subscribe(EVENTS.UPDATED, this._onRoiUpdated);
    managedViewer._roiSelectedSubscription = managedViewer.subscribe(EVENTS.UPDATED, this._onRoiSelected);
  }

  /**
   * Removes the subscriptions for the managed viewer being removed
   *
   * @param {ViewerManager} managedViewer The viewer being removed
   */
  _removeManagedViewerSubscriptions(managedViewer) {
    managedViewer._roiAddedSubscription && managedViewer._roiAddedSubscription.unsubscribe();
    managedViewer._roiModifiedSubscription && managedViewer._roiModifiedSubscription.unsubscribe();
    managedViewer._roiRemovedSubscription && managedViewer._roiRemovedSubscription.unsubscribe();
    managedViewer._roiUpdatedSubscription && managedViewer._roiUpdatedSubscription.unsubscribe();
    managedViewer._roiSelectedSubscription && managedViewer._roiSelectedSubscription.unsubscribe();
    managedViewer._roiAddedSubscription = null;
    managedViewer._roiModifiedSubscription = null;
    managedViewer._roiRemovedSubscription = null;
    managedViewer._roiUpdatedSubscription = null;
    managedViewer._roiSelectedSubscription = null;
  }

  /**
   * Returns the managed viewers that are displaying the image with the given
   * study and series UIDs
   *
   * @param {String} studyInstanceUID UID for the study
   * @param {String} seriesInstanceUID UID for the series
   *
   * @returns {Array} The managed viewers for the given series UID
   */
  _getManagedViewersForSeries(studyInstanceUID, seriesInstanceUID) {
    const filter = managedViewer => managedViewer.studyInstanceUID === studyInstanceUID && managedViewer.seriesInstanceUID === seriesInstanceUID;
    return Array.from(this.managedViewers).filter(filter);
  }

  /**
   * Returns the managed viewers that are displaying the image with the given
   * study UID
   *
   * @param {String} studyInstanceUID UID for the study
   *
   * @returns {Array} The managed viewers for the given series UID
   */
  getManagedViewersForStudy(studyInstanceUID) {
    const filter = managedViewer => managedViewer.studyInstanceUID === studyInstanceUID;
    return Array.from(this.managedViewers).filter(filter);
  }

  /**
   * Restores the created annotations for the viewer being added
   *
   * @param {ViewerManager} managedViewer The viewer being added
   */
  _restoreAnnotations(managedViewer) {
    const {
      studyInstanceUID,
      seriesInstanceUID
    } = managedViewer;
    const annotations = this.getAnnotationsForSeries(studyInstanceUID, seriesInstanceUID);
    annotations.forEach(roiAnnotation => {
      managedViewer.addRoiGraphic(roiAnnotation.roiGraphic);
    });
  }

  /**
   * Creates a managed viewer instance for the given third-party API's viewer.
   * Restores existing annotations for the given study/series.
   * Adds event subscriptions for the viewer being added.
   * Focuses the selected annotation when the viewer is being loaded into the
   * active viewport.
   *
   * @param viewer - Third-party viewer API's object to be managed
   * @param viewportId - The viewport Id where the viewer will be loaded
   * @param container - The DOM element where it will be rendered
   * @param studyInstanceUID - The study UID of the loaded image
   * @param seriesInstanceUID - The series UID of the loaded image
   * @param displaySets - All displaySets related to the same StudyInstanceUID
   *
   * @returns {ViewerManager} managed viewer
   */
  addViewer(viewer, viewportId, container, studyInstanceUID, seriesInstanceUID) {
    const managedViewer = new viewerManager(viewer, viewportId, container, studyInstanceUID, seriesInstanceUID);
    this._restoreAnnotations(managedViewer);
    viewer._manager = managedViewer;
    this.managedViewers.add(managedViewer);

    // this._potentiallyLoadSR(studyInstanceUID, displaySets);
    this._addManagedViewerSubscriptions(managedViewer);
    if (this.pendingFocus) {
      this.pendingFocus = false;
      this.focusAnnotation(this.selectedAnnotation, viewportId);
    }
    return managedViewer;
  }
  _potentiallyLoadSR(StudyInstanceUID, displaySets) {
    const studyMetadata = core_src.DicomMetadataStore.getStudy(StudyInstanceUID);
    const smDisplaySet = displaySets.find(ds => ds.Modality === 'SM');
    const {
      FrameOfReferenceUID,
      othersFrameOfReferenceUID
    } = smDisplaySet;
    if (!studyMetadata) {
      return;
    }
    let derivedDisplaySets = FrameOfReferenceUID ? displaySets.filter(ds => ds.ReferencedFrameOfReferenceUID === FrameOfReferenceUID ||
    // sometimes each depth instance has the different FrameOfReferenceID
    othersFrameOfReferenceUID.includes(ds.ReferencedFrameOfReferenceUID)) : [];
    if (!derivedDisplaySets.length) {
      return;
    }
    derivedDisplaySets = derivedDisplaySets.filter(ds => ds.Modality === 'SR');
    if (derivedDisplaySets.some(ds => ds.isLoaded === true)) {
      // Don't auto load
      return;
    }

    // find most recent and load it.
    let recentDateTime = 0;
    let recentDisplaySet = derivedDisplaySets[0];
    derivedDisplaySets.forEach(ds => {
      const dateTime = Number(`${ds.SeriesDate}${ds.SeriesTime}`);
      if (dateTime > recentDateTime) {
        recentDateTime = dateTime;
        recentDisplaySet = ds;
      }
    });
    recentDisplaySet.isLoading = true;
    recentDisplaySet.load(smDisplaySet);
  }

  /**
   * Removes the given third-party viewer API's object from the managed viewers
   * and clears all its event subscriptions
   *
   * @param {Object} viewer Third-party viewer API's object to be removed
   */
  removeViewer(viewer) {
    const managedViewer = viewer._manager;
    this._removeManagedViewerSubscriptions(managedViewer);
    managedViewer.destroy();
    this.managedViewers.delete(managedViewer);
  }

  /**
   * Toggle ROIs visibility
   */
  toggleROIsVisibility() {
    this.isROIsVisible ? this.hideROIs() : this.showROIs;
    this.isROIsVisible = !this.isROIsVisible;
  }

  /**
   * Hide all ROIs
   */
  hideROIs() {
    this.managedViewers.forEach(mv => mv.hideROIs());
  }

  /** Show all ROIs */
  showROIs() {
    this.managedViewers.forEach(mv => mv.showROIs());
  }

  /**
   * Returns a RoiAnnotation instance for the given ROI UID
   *
   * @param {String} uid UID of the annotation
   *
   * @returns {RoiAnnotation} The RoiAnnotation instance found for the given UID
   */
  getAnnotation(uid) {
    return this.annotations[uid];
  }

  /**
   * Returns all the RoiAnnotation instances being managed
   *
   * @returns {Array} All RoiAnnotation instances
   */
  getAnnotations() {
    const annotations = [];
    Object.keys(this.annotations).forEach(uid => {
      annotations.push(this.getAnnotation(uid));
    });
    return annotations;
  }

  /**
   * Returns the RoiAnnotation instances registered with the given study UID
   *
   * @param {String} studyInstanceUID UID for the study
   */
  getAnnotationsForStudy(studyInstanceUID) {
    const filter = a => a.studyInstanceUID === studyInstanceUID;
    return this.getAnnotations().filter(filter);
  }

  /**
   * Returns the RoiAnnotation instances registered with the given study and
   * series UIDs
   *
   * @param {String} studyInstanceUID UID for the study
   * @param {String} seriesInstanceUID UID for the series
   */
  getAnnotationsForSeries(studyInstanceUID, seriesInstanceUID) {
    const filter = annotation => annotation.studyInstanceUID === studyInstanceUID && annotation.seriesInstanceUID === seriesInstanceUID;
    return this.getAnnotations().filter(filter);
  }

  /**
   * Returns the selected RoiAnnotation instance or null if none is selected
   *
   * @returns {RoiAnnotation} The selected RoiAnnotation instance
   */
  getSelectedAnnotation() {
    return this.selectedAnnotation;
  }

  /**
   * Clear current RoiAnnotation selection
   */
  clearSelection() {
    if (this.selectedAnnotation) {
      this.setROIStyle(this.selectedAnnotation.uid, {
        stroke: {
          color: '#00ff00'
        }
      });
    }
    this.selectedAnnotation = null;
  }

  /**
   * Selects the given RoiAnnotation instance, publishing an ANNOTATION_SELECTED
   * event to notify all the subscribers
   *
   * @param {RoiAnnotation} roiAnnotation The instance to be selected
   */
  selectAnnotation(roiAnnotation) {
    if (this.selectedAnnotation) {
      this.clearSelection();
    }
    this.selectedAnnotation = roiAnnotation;
    this._broadcastEvent(MicroscopyService_EVENTS.ANNOTATION_SELECTED, roiAnnotation);
    this.setROIStyle(roiAnnotation.uid, utils_styles.active);
  }

  /**
   * Toggles overview map
   *
   * @param viewportId The active viewport index
   * @returns {void}
   */
  toggleOverviewMap(viewportId) {
    const managedViewers = Array.from(this.managedViewers);
    const managedViewer = managedViewers.find(mv => mv.viewportId === viewportId);
    if (managedViewer) {
      managedViewer.toggleOverviewMap();
    }
  }

  /**
   * Removes a RoiAnnotation instance from the managed annotations and reflects
   * its removal on all third-party viewers being managed
   *
   * @param {RoiAnnotation} roiAnnotation The instance to be removed
   */
  removeAnnotation(roiAnnotation) {
    const {
      uid,
      studyInstanceUID,
      seriesInstanceUID
    } = roiAnnotation;
    const filter = managedViewer => managedViewer.studyInstanceUID === studyInstanceUID && managedViewer.seriesInstanceUID === seriesInstanceUID;
    const managedViewers = Array.from(this.managedViewers).filter(filter);
    managedViewers.forEach(managedViewer => managedViewer.removeRoiGraphic(uid));
    if (this.annotations[uid]) {
      this.roiUids.delete(uid);
      this.annotations[uid].destroy();
      delete this.annotations[uid];
      this._broadcastEvent(MicroscopyService_EVENTS.ANNOTATION_REMOVED, roiAnnotation);
    }
  }

  /**
   * Focus the given RoiAnnotation instance by changing the OpenLayers' Map view
   * state of the managed viewer with the given viewport index.
   * If the image for the given annotation is not yet loaded into the viewport,
   * it will set a pendingFocus flag to true in order to perform the focus when
   * the managed viewer instance is created.
   *
   * @param {RoiAnnotation} roiAnnotation RoiAnnotation instance to be focused
   * @param {string} viewportId Index of the viewport to focus
   */
  focusAnnotation(roiAnnotation, viewportId) {
    const filter = mv => mv.viewportId === viewportId;
    const managedViewer = Array.from(this.managedViewers).find(filter);
    if (managedViewer) {
      managedViewer.setViewStateByExtent(roiAnnotation);
    } else {
      this.pendingFocus = true;
    }
  }

  /**
   * Synchronize the ROI graphics for all the managed viewers that has the same
   * series UID of the given managed viewer
   *
   * @param {ViewerManager} baseManagedViewer Reference managed viewer
   */
  synchronizeViewers(baseManagedViewer) {
    const {
      studyInstanceUID,
      seriesInstanceUID
    } = baseManagedViewer;
    const managedViewers = this._getManagedViewersForSeries(studyInstanceUID, seriesInstanceUID);

    // Prevent infinite loops arrising from updates.
    managedViewers.forEach(managedViewer => this._removeManagedViewerSubscriptions(managedViewer));
    managedViewers.forEach(managedViewer => {
      if (managedViewer === baseManagedViewer) {
        return;
      }
      const annotations = this.getAnnotationsForSeries(studyInstanceUID, seriesInstanceUID);
      managedViewer.clearRoiGraphics();
      annotations.forEach(roiAnnotation => {
        managedViewer.addRoiGraphic(roiAnnotation.roiGraphic);
      });
    });
    managedViewers.forEach(managedViewer => this._addManagedViewerSubscriptions(managedViewer));
  }

  /**
   * Activates interactions across all the viewers being managed
   *
   * @param {Array} interactions interactions
   */
  activateInteractions(interactions) {
    this.managedViewers.forEach(mv => mv.activateInteractions(interactions));
    this.activeInteractions = interactions;
  }

  /**
   * Triggers the relabelling process for the given RoiAnnotation instance, by
   * publishing the RELABEL event to notify the subscribers
   *
   * @param {RoiAnnotation} roiAnnotation The instance to be relabelled
   * @param {boolean} newAnnotation Whether the annotation is newly drawn (so it deletes on cancel).
   */
  triggerRelabel(roiAnnotation) {
    let newAnnotation = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : false;
    let onRelabel = arguments.length > 2 ? arguments[2] : undefined;
    if (!onRelabel) {
      onRelabel = _ref => {
        let {
          label
        } = _ref;
        return this.managedViewers.forEach(mv => mv.updateROIProperties({
          uid: roiAnnotation.uid,
          properties: {
            label
          }
        }));
      };
    }
    this._broadcastEvent(MicroscopyService_EVENTS.RELABEL, {
      roiAnnotation,
      deleteCallback: () => this.removeAnnotation(roiAnnotation),
      successCallback: onRelabel,
      newAnnotation
    });
  }

  /**
   * Triggers the deletion process for the given RoiAnnotation instance, by
   * publishing the DELETE event to notify the subscribers
   *
   * @param {RoiAnnotation} roiAnnotation The instance to be deleted
   */
  triggerDelete(roiAnnotation) {
    this._broadcastEvent(MicroscopyService_EVENTS.DELETE, roiAnnotation);
  }

  /**
   * Set ROI style for all managed viewers
   *
   * @param {string} uid The ROI uid that will be styled
   * @param {object} styleOptions - Style options
   * @param {object*} styleOptions.stroke - Style options for the outline of the geometry
   * @param {number[]} styleOptions.stroke.color - RGBA color of the outline
   * @param {number} styleOptions.stroke.width - Width of the outline
   * @param {object*} styleOptions.fill - Style options for body the geometry
   * @param {number[]} styleOptions.fill.color - RGBA color of the body
   * @param {object*} styleOptions.image - Style options for image
   */
  setROIStyle(uid, styleOptions) {
    this.managedViewers.forEach(mv => mv.setROIStyle(uid, styleOptions));
  }
}
MicroscopyService.REGISTRATION = serviceManager => {
  return {
    name: 'microscopyService',
    altName: 'MicroscopyService',
    create: _ref2 => {
      let {
        configuration = {}
      } = _ref2;
      return new MicroscopyService(serviceManager);
    }
  };
};

// EXTERNAL MODULE: ../../../node_modules/dcmjs/build/dcmjs.es.js
var dcmjs_es = __webpack_require__(67540);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/callInputDialog.tsx



/**
 *
 * @param {*} data
 * @param {*} data.text
 * @param {*} data.label
 * @param {*} event
 * @param {func} callback
 * @param {*} isArrowAnnotateInputDialog
 */
function callInputDialog(_ref) {
  let {
    uiDialogService,
    title = 'Annotation',
    defaultValue = '',
    callback = (value, action) => {}
  } = _ref;
  const dialogId = 'microscopy-input-dialog';
  const onSubmitHandler = _ref2 => {
    let {
      action,
      value
    } = _ref2;
    switch (action.id) {
      case 'save':
        callback(value.value, action.id);
        break;
      case 'cancel':
        callback('', action.id);
        break;
    }
    uiDialogService.dismiss({
      id: dialogId
    });
  };
  if (uiDialogService) {
    uiDialogService.create({
      id: dialogId,
      centralize: true,
      isDraggable: false,
      showOverlay: true,
      content: src/* Dialog */.Vq,
      contentProps: {
        title: title,
        value: {
          value: defaultValue
        },
        noCloseButton: true,
        onClose: () => uiDialogService.dismiss({
          id: dialogId
        }),
        actions: [{
          id: 'cancel',
          text: 'Cancel',
          type: src/* ButtonEnums.type */.LZ.dt.secondary
        }, {
          id: 'save',
          text: 'Save',
          type: src/* ButtonEnums.type */.LZ.dt.primary
        }],
        onSubmit: onSubmitHandler,
        body: _ref3 => {
          let {
            value,
            setValue
          } = _ref3;
          return /*#__PURE__*/react.createElement(src/* Input */.II, {
            label: "Enter your annotation",
            labelClassName: "text-white text-[14px] leading-[1.2]",
            autoFocus: true,
            className: "border-primary-main bg-black",
            type: "text",
            value: value.defaultValue,
            onChange: event => {
              event.persist();
              setValue(value => ({
                ...value,
                value: event.target.value
              }));
            },
            onKeyPress: event => {
              if (event.key === 'Enter') {
                onSubmitHandler({
                  value,
                  action: {
                    id: 'save'
                  }
                });
              }
            }
          });
        }
      }
    });
  }
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/DEVICE_OBSERVER_UID.js
// We need to define a UID for this extension as a device, and it should be the same for all saves:

const uid = '2.25.285241207697168520771311899641885187923';
/* harmony default export */ const DEVICE_OBSERVER_UID = (uid);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/constructSR.ts



/**
 *
 * @param {*} metadata - Microscopy Image instance metadata
 * @param {*} SeriesDescription - SR description
 * @param {*} annotations - Annotations
 *
 * @return Comprehensive3DSR dataset
 */
function constructSR(metadata, _ref, annotations) {
  let {
    SeriesDescription,
    SeriesNumber
  } = _ref;
  // Handle malformed data
  if (!metadata.SpecimenDescriptionSequence) {
    metadata.SpecimenDescriptionSequence = {
      SpecimenUID: metadata.SeriesInstanceUID,
      SpecimenIdentifier: metadata.SeriesDescription
    };
  }
  const {
    SpecimenDescriptionSequence
  } = metadata;

  // construct Comprehensive3DSR dataset
  const observationContext = new dcmjs_es["default"].sr.templates.ObservationContext({
    observerPersonContext: new dcmjs_es["default"].sr.templates.ObserverContext({
      observerType: new dcmjs_es["default"].sr.coding.CodedConcept({
        value: '121006',
        schemeDesignator: 'DCM',
        meaning: 'Person'
      }),
      observerIdentifyingAttributes: new dcmjs_es["default"].sr.templates.PersonObserverIdentifyingAttributes({
        name: '@ohif/extension-dicom-microscopy'
      })
    }),
    observerDeviceContext: new dcmjs_es["default"].sr.templates.ObserverContext({
      observerType: new dcmjs_es["default"].sr.coding.CodedConcept({
        value: '121007',
        schemeDesignator: 'DCM',
        meaning: 'Device'
      }),
      observerIdentifyingAttributes: new dcmjs_es["default"].sr.templates.DeviceObserverIdentifyingAttributes({
        uid: DEVICE_OBSERVER_UID
      })
    }),
    subjectContext: new dcmjs_es["default"].sr.templates.SubjectContext({
      subjectClass: new dcmjs_es["default"].sr.coding.CodedConcept({
        value: '121027',
        schemeDesignator: 'DCM',
        meaning: 'Specimen'
      }),
      subjectClassSpecificContext: new dcmjs_es["default"].sr.templates.SubjectContextSpecimen({
        uid: SpecimenDescriptionSequence.SpecimenUID,
        identifier: SpecimenDescriptionSequence.SpecimenIdentifier || metadata.SeriesInstanceUID,
        containerIdentifier: metadata.ContainerIdentifier || metadata.SeriesInstanceUID
      })
    })
  });
  const imagingMeasurements = [];
  for (let i = 0; i < annotations.length; i++) {
    const {
      roiGraphic: roi,
      label
    } = annotations[i];
    let {
      measurements,
      evaluations,
      marker,
      presentationState
    } = roi.properties;
    console.log('[SR] storing marker...', marker);
    console.log('[SR] storing measurements...', measurements);
    console.log('[SR] storing evaluations...', evaluations);
    console.log('[SR] storing presentation state...', presentationState);
    if (presentationState) {
      presentationState.marker = marker;
    }

    /** Avoid incompatibility with dcmjs */
    measurements = measurements.map(measurement => {
      const ConceptName = Array.isArray(measurement.ConceptNameCodeSequence) ? measurement.ConceptNameCodeSequence[0] : measurement.ConceptNameCodeSequence;
      const MeasuredValue = Array.isArray(measurement.MeasuredValueSequence) ? measurement.MeasuredValueSequence[0] : measurement.MeasuredValueSequence;
      const MeasuredValueUnits = Array.isArray(MeasuredValue.MeasurementUnitsCodeSequence) ? MeasuredValue.MeasurementUnitsCodeSequence[0] : MeasuredValue.MeasurementUnitsCodeSequence;
      return new dcmjs_es["default"].sr.valueTypes.NumContentItem({
        name: new dcmjs_es["default"].sr.coding.CodedConcept({
          meaning: ConceptName.CodeMeaning,
          value: ConceptName.CodeValue,
          schemeDesignator: ConceptName.CodingSchemeDesignator
        }),
        value: MeasuredValue.NumericValue,
        unit: new dcmjs_es["default"].sr.coding.CodedConcept({
          value: MeasuredValueUnits.CodeValue,
          meaning: MeasuredValueUnits.CodeMeaning,
          schemeDesignator: MeasuredValueUnits.CodingSchemeDesignator
        })
      });
    });

    /** Avoid incompatibility with dcmjs */
    evaluations = evaluations.map(evaluation => {
      const ConceptName = Array.isArray(evaluation.ConceptNameCodeSequence) ? evaluation.ConceptNameCodeSequence[0] : evaluation.ConceptNameCodeSequence;
      return new dcmjs_es["default"].sr.valueTypes.TextContentItem({
        name: new dcmjs_es["default"].sr.coding.CodedConcept({
          value: ConceptName.CodeValue,
          meaning: ConceptName.CodeMeaning,
          schemeDesignator: ConceptName.CodingSchemeDesignator
        }),
        value: evaluation.TextValue,
        relationshipType: evaluation.RelationshipType
      });
    });
    const identifier = `ROI #${i + 1}`;
    const group = new dcmjs_es["default"].sr.templates.PlanarROIMeasurementsAndQualitativeEvaluations({
      trackingIdentifier: new dcmjs_es["default"].sr.templates.TrackingIdentifier({
        uid: roi.uid,
        identifier: presentationState ? identifier.concat(`(${JSON.stringify(presentationState)})`) : identifier
      }),
      referencedRegion: new dcmjs_es["default"].sr.contentItems.ImageRegion3D({
        graphicType: roi.scoord3d.graphicType,
        graphicData: roi.scoord3d.graphicData,
        frameOfReferenceUID: roi.scoord3d.frameOfReferenceUID
      }),
      findingType: new dcmjs_es["default"].sr.coding.CodedConcept({
        value: label,
        schemeDesignator: '@ohif/extension-dicom-microscopy',
        meaning: 'FREETEXT'
      }),
      /** Evaluations will conflict with current tracking identifier */
      /** qualitativeEvaluations: evaluations, */
      measurements
    });
    imagingMeasurements.push(...group);
  }
  const measurementReport = new dcmjs_es["default"].sr.templates.MeasurementReport({
    languageOfContentItemAndDescendants: new dcmjs_es["default"].sr.templates.LanguageOfContentItemAndDescendants({}),
    observationContext,
    procedureReported: new dcmjs_es["default"].sr.coding.CodedConcept({
      value: '112703',
      schemeDesignator: 'DCM',
      meaning: 'Whole Slide Imaging'
    }),
    imagingMeasurements
  });
  const dataset = new dcmjs_es["default"].sr.documents.Comprehensive3DSR({
    content: measurementReport[0],
    evidence: [metadata],
    seriesInstanceUID: dcmjs_es["default"].data.DicomMetaDictionary.uid(),
    seriesNumber: SeriesNumber,
    seriesDescription: SeriesDescription || 'Whole slide imaging structured report',
    sopInstanceUID: dcmjs_es["default"].data.DicomMetaDictionary.uid(),
    instanceNumber: 1,
    manufacturer: 'dcmjs-org'
  });
  dataset.SpecificCharacterSet = 'ISO_IR 192';
  const fileMetaInformationVersionArray = new Uint8Array(2);
  fileMetaInformationVersionArray[1] = 1;
  dataset._meta = {
    FileMetaInformationVersion: {
      Value: [fileMetaInformationVersionArray.buffer],
      // TODO
      vr: 'OB'
    },
    MediaStorageSOPClassUID: dataset.sopClassUID,
    MediaStorageSOPInstanceUID: dataset.sopInstanceUID,
    TransferSyntaxUID: {
      Value: ['1.2.840.10008.1.2.1'],
      vr: 'UI'
    },
    ImplementationClassUID: {
      Value: [dcmjs_es["default"].data.DicomMetaDictionary.uid()],
      vr: 'UI'
    },
    ImplementationVersionName: {
      Value: ['@ohif/extension-dicom-microscopy'],
      vr: 'SH'
    }
  };
  return dataset;
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/saveByteArray.ts
/**
 * Trigger file download from an array buffer
 * @param buffer
 * @param filename
 */
function saveByteArray(buffer, filename) {
  const blob = new Blob([buffer], {
    type: 'application/dicom'
  });
  const link = document.createElement('a');
  link.href = window.URL.createObjectURL(blob);
  link.download = filename;
  link.click();
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/components/MicroscopyPanel/MicroscopyPanel.tsx









let saving = false;
const {
  datasetToBuffer
} = dcmjs_es["default"].data;
const formatArea = area => {
  let mult = 1;
  let unit = 'mm';
  if (area > 1000000) {
    unit = 'm';
    mult = 1 / 1000000;
  } else if (area < 1) {
    unit = 'μm';
    mult = 1000000;
  }
  return `${(area * mult).toFixed(2)} ${unit}²`;
};
const formatLength = (length, unit) => {
  let mult = 1;
  if (unit == 'km' || !unit && length > 1000000) {
    unit = 'km';
    mult = 1 / 1000000;
  } else if (unit == 'm' || !unit && length > 1000) {
    unit = 'm';
    mult = 1 / 1000;
  } else if (unit == 'μm' || !unit && length < 1) {
    unit = 'μm';
    mult = 1000;
  } else if (unit && unit != 'mm') {
    throw new Error(`Unknown length unit ${unit}`);
  } else {
    unit = 'mm';
  }
  return `${(length * mult).toFixed(2)} ${unit}`;
};
/**
 * Microscopy Measurements Panel Component
 *
 * @param props
 * @returns
 */
function MicroscopyPanel(props) {
  const {
    microscopyService
  } = props.servicesManager.services;
  const [studyInstanceUID, setStudyInstanceUID] = (0,react.useState)(null);
  const [roiAnnotations, setRoiAnnotations] = (0,react.useState)([]);
  const [selectedAnnotation, setSelectedAnnotation] = (0,react.useState)(null);
  const {
    servicesManager,
    extensionManager
  } = props;
  const {
    uiDialogService,
    displaySetService
  } = servicesManager.services;
  (0,react.useEffect)(() => {
    const viewport = props.viewports.get(props.activeViewportId);
    if (viewport?.displaySetInstanceUIDs[0]) {
      const displaySet = displaySetService.getDisplaySetByUID(viewport.displaySetInstanceUIDs[0]);
      if (displaySet) {
        setStudyInstanceUID(displaySet.StudyInstanceUID);
      }
    }
  }, [props.viewports, props.activeViewportId]);
  (0,react.useEffect)(() => {
    const onAnnotationUpdated = () => {
      const roiAnnotations = microscopyService.getAnnotationsForStudy(studyInstanceUID);
      setRoiAnnotations(roiAnnotations);
    };
    const onAnnotationSelected = () => {
      const selectedAnnotation = microscopyService.getSelectedAnnotation();
      setSelectedAnnotation(selectedAnnotation);
    };
    const onAnnotationRemoved = () => {
      onAnnotationUpdated();
    };
    const {
      unsubscribe: unsubscribeAnnotationUpdated
    } = microscopyService.subscribe(MicroscopyService_EVENTS.ANNOTATION_UPDATED, onAnnotationUpdated);
    const {
      unsubscribe: unsubscribeAnnotationSelected
    } = microscopyService.subscribe(MicroscopyService_EVENTS.ANNOTATION_SELECTED, onAnnotationSelected);
    const {
      unsubscribe: unsubscribeAnnotationRemoved
    } = microscopyService.subscribe(MicroscopyService_EVENTS.ANNOTATION_REMOVED, onAnnotationRemoved);
    onAnnotationUpdated();
    onAnnotationSelected();

    // on unload unsubscribe from events
    return () => {
      unsubscribeAnnotationUpdated();
      unsubscribeAnnotationSelected();
      unsubscribeAnnotationRemoved();
    };
  }, [studyInstanceUID]);

  /**
   * On clicking "Save Annotations" button, prompt an input modal for the
   * new series' description, and continue to save.
   *
   * @returns
   */
  const promptSave = () => {
    const annotations = microscopyService.getAnnotationsForStudy(studyInstanceUID);
    if (!annotations || saving) {
      return;
    }
    callInputDialog({
      uiDialogService,
      title: 'Enter description of the Series',
      defaultValue: '',
      callback: (value, action) => {
        switch (action) {
          case 'save':
            {
              saveFunction(value);
            }
        }
      }
    });
  };
  const getAllDisplaySets = studyMetadata => {
    let allDisplaySets = [];
    studyMetadata.series.forEach(series => {
      const displaySets = displaySetService.getDisplaySetsForSeries(series.SeriesInstanceUID);
      allDisplaySets = allDisplaySets.concat(displaySets);
    });
    return allDisplaySets;
  };

  /**
   * Save annotations as a series
   *
   * @param SeriesDescription - series description
   * @returns
   */
  const saveFunction = async SeriesDescription => {
    const dataSource = extensionManager.getActiveDataSource()[0];
    const {
      onSaveComplete
    } = props;
    const annotations = microscopyService.getAnnotationsForStudy(studyInstanceUID);
    saving = true;

    // There is only one viewer possible for one study,
    // Since once study contains multiple resolution levels (series) of one whole
    // Slide image.

    const studyMetadata = core_src.DicomMetadataStore.getStudy(studyInstanceUID);
    const displaySets = getAllDisplaySets(studyMetadata);
    const smDisplaySet = displaySets.find(ds => ds.Modality === 'SM');

    // Get the next available series number after 4700.

    const dsWithMetadata = displaySets.filter(ds => ds.metadata && ds.metadata.SeriesNumber && typeof ds.metadata.SeriesNumber === 'number');

    // Generate next series number
    const seriesNumbers = dsWithMetadata.map(ds => ds.metadata.SeriesNumber);
    const maxSeriesNumber = Math.max(...seriesNumbers, 4700);
    const SeriesNumber = maxSeriesNumber + 1;
    const {
      instance: metadata
    } = smDisplaySet;

    // construct SR dataset
    const dataset = constructSR(metadata, {
      SeriesDescription,
      SeriesNumber
    }, annotations);

    // Save in DICOM format
    try {
      if (dataSource) {
        if (dataSource.wadoRoot == 'saveDicom') {
          // download as DICOM file
          const part10Buffer = datasetToBuffer(dataset);
          saveByteArray(part10Buffer, `sr-microscopy.dcm`);
        } else {
          // Save into Web Data source
          const {
            StudyInstanceUID
          } = dataset;
          await dataSource.store.dicom(dataset);
          if (StudyInstanceUID) {
            dataSource.deleteStudyMetadataPromise(StudyInstanceUID);
          }
        }
        onSaveComplete({
          title: 'SR Saved',
          message: 'Measurements downloaded successfully',
          type: 'success'
        });
      } else {
        console.error('Server unspecified');
      }
    } catch (error) {
      onSaveComplete({
        title: 'SR Save Failed',
        message: error.message || error.toString(),
        type: 'error'
      });
    } finally {
      saving = false;
    }
  };

  /**
   * On clicking "Reject annotations" button
   */
  const onDeleteCurrentSRHandler = async () => {
    try {
      const activeViewport = props.viewports[props.activeViewportId];
      const {
        StudyInstanceUID
      } = activeViewport;

      // TODO: studies?
      const study = core_src.DicomMetadataStore.getStudy(StudyInstanceUID);
      const lastDerivedDisplaySet = study.derivedDisplaySets.sort((ds1, ds2) => {
        const dateTime1 = Number(`${ds1.SeriesDate}${ds1.SeriesTime}`);
        const dateTime2 = Number(`${ds2.SeriesDate}${ds2.SeriesTime}`);
        return dateTime1 > dateTime2;
      })[study.derivedDisplaySets.length - 1];

      // TODO: use dataSource.reject.dicom()
      // await DICOMSR.rejectMeasurements(
      //   study.wadoRoot,
      //   lastDerivedDisplaySet.StudyInstanceUID,
      //   lastDerivedDisplaySet.SeriesInstanceUID
      // );
      props.onRejectComplete({
        title: 'Report rejected',
        message: 'Latest report rejected successfully',
        type: 'success'
      });
    } catch (error) {
      props.onRejectComplete({
        title: 'Failed to reject report',
        message: error.message,
        type: 'error'
      });
    }
  };

  /**
   * Handler for clicking event of an annotation item.
   *
   * @param param0
   */
  const onMeasurementItemClickHandler = _ref => {
    let {
      uid
    } = _ref;
    const roiAnnotation = microscopyService.getAnnotation(uid);
    microscopyService.selectAnnotation(roiAnnotation);
    microscopyService.focusAnnotation(roiAnnotation, props.activeViewportId);
  };

  /**
   * Handler for "Edit" action of an annotation item
   * @param param0
   */
  const onMeasurementItemEditHandler = _ref2 => {
    let {
      uid,
      isActive
    } = _ref2;
    props.commandsManager.runCommand('setLabel', {
      uid
    }, 'MICROSCOPY');
  };

  // Convert ROI annotations managed by microscopyService into our
  // own format for display
  const data = roiAnnotations.map((roiAnnotation, index) => {
    const label = roiAnnotation.getDetailedLabel();
    const area = roiAnnotation.getArea();
    const length = roiAnnotation.getLength();
    const shortAxisLength = roiAnnotation.roiGraphic.properties.shortAxisLength;
    const isSelected = selectedAnnotation === roiAnnotation;

    // other events
    const {
      uid
    } = roiAnnotation;

    // display text
    const displayText = [];
    if (area !== undefined) {
      displayText.push(formatArea(area));
    } else if (length !== undefined) {
      displayText.push(shortAxisLength ? `${formatLength(length, 'μm')} x ${formatLength(shortAxisLength, 'μm')}` : `${formatLength(length, 'μm')}`);
    }

    // convert to measurementItem format compatible with <MeasurementTable /> component
    return {
      uid,
      index,
      label,
      isActive: isSelected,
      displayText,
      roiAnnotation
    };
  });
  const disabled = data.length === 0;
  return /*#__PURE__*/react.createElement(react.Fragment, null, /*#__PURE__*/react.createElement("div", {
    className: "ohif-scrollbar overflow-y-auto overflow-x-hidden",
    "data-cy": 'measurements-panel'
  }, /*#__PURE__*/react.createElement(src/* MeasurementTable */.wt, {
    title: "Measurements",
    servicesManager: props.servicesManager,
    data: data,
    onClick: onMeasurementItemClickHandler,
    onEdit: onMeasurementItemEditHandler
  })));
}
const connectedMicroscopyPanel = (0,es/* withTranslation */.Zh)(['MicroscopyTable', 'Common'])(MicroscopyPanel);
/* harmony default export */ const MicroscopyPanel_MicroscopyPanel = (connectedMicroscopyPanel);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/getPanelModule.tsx




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
  const wrappedMeasurementPanel = () => {
    const [{
      activeViewportId,
      viewports
    }] = (0,src/* useViewportGrid */.O_)();
    return /*#__PURE__*/react.createElement(MicroscopyPanel_MicroscopyPanel, {
      viewports: viewports,
      activeViewportId: activeViewportId,
      onSaveComplete: () => {},
      onRejectComplete: () => {},
      commandsManager: commandsManager,
      servicesManager: servicesManager,
      extensionManager: extensionManager
    });
  };
  return [{
    name: 'measure',
    iconName: 'tab-linear',
    iconLabel: 'Measure',
    label: 'Measurements',
    secondaryLabel: 'Measurements',
    component: wrappedMeasurementPanel
  }];
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/getCommandsModule.ts


function getCommandsModule(_ref) {
  let {
    servicesManager,
    commandsManager,
    extensionManager
  } = _ref;
  const {
    viewportGridService,
    uiDialogService,
    microscopyService
  } = servicesManager.services;
  const actions = {
    // Measurement tool commands:
    deleteMeasurement: _ref2 => {
      let {
        uid
      } = _ref2;
      if (uid) {
        const roiAnnotation = microscopyService.getAnnotation(uid);
        if (roiAnnotation) {
          microscopyService.removeAnnotation(roiAnnotation);
        }
      }
    },
    setLabel: _ref3 => {
      let {
        uid
      } = _ref3;
      const roiAnnotation = microscopyService.getAnnotation(uid);
      callInputDialog({
        uiDialogService,
        defaultValue: '',
        callback: (value, action) => {
          switch (action) {
            case 'save':
              {
                roiAnnotation.setLabel(value);
                microscopyService.triggerRelabel(roiAnnotation);
              }
          }
        }
      });
    },
    setToolActive: _ref4 => {
      let {
        toolName,
        toolGroupId = 'MICROSCOPY'
      } = _ref4;
      const dragPanOnMiddle = ['dragPan', {
        bindings: {
          mouseButtons: ['middle']
        }
      }];
      const dragZoomOnRight = ['dragZoom', {
        bindings: {
          mouseButtons: ['right']
        }
      }];
      if (['line', 'box', 'circle', 'point', 'polygon', 'freehandpolygon', 'freehandline'].indexOf(toolName) >= 0) {
        // TODO: read from configuration
        const options = {
          geometryType: toolName,
          vertexEnabled: true,
          styleOptions: utils_styles["default"],
          bindings: {
            mouseButtons: ['left']
          }
        };
        if ('line' === toolName) {
          options.minPoints = 2;
          options.maxPoints = 2;
        } else if ('point' === toolName) {
          delete options.styleOptions;
          delete options.vertexEnabled;
        }
        microscopyService.activateInteractions([['draw', options], dragPanOnMiddle, dragZoomOnRight]);
      } else if (toolName == 'dragPan') {
        microscopyService.activateInteractions([['dragPan', {
          bindings: {
            mouseButtons: ['left', 'middle']
          }
        }], dragZoomOnRight]);
      } else {
        microscopyService.activateInteractions([[toolName, {
          bindings: {
            mouseButtons: ['left']
          }
        }], dragPanOnMiddle, dragZoomOnRight]);
      }
    },
    toggleOverlays: () => {
      // overlay
      const overlays = document.getElementsByClassName('microscopy-viewport-overlay');
      let onoff = false; // true if this will toggle on
      for (let i = 0; i < overlays.length; i++) {
        if (i === 0) {
          onoff = overlays.item(0).classList.contains('hidden');
        }
        overlays.item(i).classList.toggle('hidden');
      }

      // overview
      const {
        activeViewportId,
        viewports
      } = viewportGridService.getState();
      microscopyService.toggleOverviewMap(activeViewportId);
    },
    toggleAnnotations: () => {
      microscopyService.toggleROIsVisibility();
    }
  };
  const definitions = {
    deleteMeasurement: {
      commandFn: actions.deleteMeasurement,
      storeContexts: [],
      options: {}
    },
    setLabel: {
      commandFn: actions.setLabel,
      storeContexts: [],
      options: {}
    },
    setToolActive: {
      commandFn: actions.setToolActive,
      storeContexts: [],
      options: {}
    },
    toggleOverlays: {
      commandFn: actions.toggleOverlays,
      storeContexts: [],
      options: {}
    },
    toggleAnnotations: {
      commandFn: actions.toggleAnnotations,
      storeContexts: [],
      options: {}
    }
  };
  return {
    actions,
    definitions,
    defaultContext: 'MICROSCOPY'
  };
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/DicomMicroscopySopClassHandler.js

const {
  utils
} = core_src["default"];
const SOP_CLASS_UIDS = {
  VL_WHOLE_SLIDE_MICROSCOPY_IMAGE_STORAGE: '1.2.840.10008.5.1.4.1.1.77.1.6'
};
const SOPClassHandlerId = '@ohif/extension-dicom-microscopy.sopClassHandlerModule.DicomMicroscopySopClassHandler';
function _getDisplaySetsFromSeries(instances, servicesManager, extensionManager) {
  // If the series has no instances, stop here
  if (!instances || !instances.length) {
    throw new Error('No instances were provided');
  }
  const instance = instances[0];
  let singleFrameInstance = instance;
  let currentFrames = +singleFrameInstance.NumberOfFrames || 1;
  for (const instanceI of instances) {
    const framesI = +instanceI.NumberOfFrames || 1;
    if (framesI < currentFrames) {
      singleFrameInstance = instanceI;
      currentFrames = framesI;
    }
  }
  let imageIdForThumbnail = null;
  if (singleFrameInstance) {
    if (currentFrames == 1) {
      // Not all DICOM server implementations support thumbnail service,
      // So if we have a single-frame image, we will prefer it.
      imageIdForThumbnail = singleFrameInstance.imageId;
    }
    if (!imageIdForThumbnail) {
      // use the thumbnail service provided by DICOM server
      const dataSource = extensionManager.getActiveDataSource()[0];
      imageIdForThumbnail = dataSource.getImageIdsForInstance({
        instance: singleFrameInstance,
        thumbnail: true
      });
    }
  }
  const {
    FrameOfReferenceUID,
    SeriesDescription,
    ContentDate,
    ContentTime,
    SeriesNumber,
    StudyInstanceUID,
    SeriesInstanceUID,
    SOPInstanceUID,
    SOPClassUID
  } = instance;
  instances = instances.map(inst => {
    // NOTE: According to DICOM standard a series should have a FrameOfReferenceUID
    // When the Microscopy file was built by certain tool from multiple image files,
    // each instance's FrameOfReferenceUID is sometimes different.
    // Even though this means the file was not well formatted DICOM VL Whole Slide Microscopy Image,
    // the case is so often, so let's override this value manually here.
    //
    // https://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.7.4.html#sect_C.7.4.1.1.1

    inst.FrameOfReferenceUID = instance.FrameOfReferenceUID;
    return inst;
  });
  const othersFrameOfReferenceUID = instances.filter(v => v).map(inst => inst.FrameOfReferenceUID).filter((value, index, array) => array.indexOf(value) === index);
  if (othersFrameOfReferenceUID.length > 1) {
    console.warn('Expected FrameOfReferenceUID of difference instances within a series to be the same, found multiple different values', othersFrameOfReferenceUID);
  }
  const displaySet = {
    plugin: 'microscopy',
    Modality: 'SM',
    altImageText: 'Microscopy',
    displaySetInstanceUID: utils.guid(),
    SOPInstanceUID,
    SeriesInstanceUID,
    StudyInstanceUID,
    FrameOfReferenceUID,
    SOPClassHandlerId,
    SOPClassUID,
    SeriesDescription: SeriesDescription || 'Microscopy Data',
    // Map ContentDate/Time to SeriesTime for series list sorting.
    SeriesDate: ContentDate,
    SeriesTime: ContentTime,
    SeriesNumber,
    firstInstance: singleFrameInstance,
    // top level instance in the image Pyramid
    instance,
    numImageFrames: 0,
    numInstances: 1,
    imageIdForThumbnail,
    // thumbnail image
    others: instances,
    // all other level instances in the image Pyramid
    othersFrameOfReferenceUID
  };
  return [displaySet];
}
function getDicomMicroscopySopClassHandler(_ref) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const getDisplaySetsFromSeries = instances => {
    return _getDisplaySetsFromSeries(instances, servicesManager, extensionManager);
  };
  return {
    name: 'DicomMicroscopySopClassHandler',
    sopClassUids: [SOP_CLASS_UIDS.VL_WHOLE_SLIDE_MICROSCOPY_IMAGE_STORAGE],
    getDisplaySetsFromSeries
  };
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/dcmCodeValues.js
const DCM_CODE_VALUES = {
  IMAGING_MEASUREMENTS: '126010',
  MEASUREMENT_GROUP: '125007',
  IMAGE_REGION: '111030',
  FINDING: '121071',
  TRACKING_UNIQUE_IDENTIFIER: '112039',
  LENGTH: '410668003',
  AREA: '42798000',
  SHORT_AXIS: 'G-A186',
  LONG_AXIS: 'G-A185',
  ELLIPSE_AREA: 'G-D7FE' // TODO: Remove this
};

/* harmony default export */ const dcmCodeValues = (DCM_CODE_VALUES);
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/toArray.js
function toArray(item) {
  return Array.isArray(item) ? item : [item];
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/loadSR.js



const MeasurementReport = dcmjs_es["default"].adapters.DICOMMicroscopyViewer.MeasurementReport;

// Define as async so that it returns a promise, expected by the ViewportGrid
async function loadSR(microscopyService, microscopySRDisplaySet, referencedDisplaySet) {
  const naturalizedDataset = microscopySRDisplaySet.metadata;
  const {
    StudyInstanceUID,
    FrameOfReferenceUID
  } = referencedDisplaySet;
  const managedViewers = microscopyService.getManagedViewersForStudy(StudyInstanceUID);
  if (!managedViewers || !managedViewers.length) {
    return;
  }
  microscopySRDisplaySet.isLoaded = true;
  const {
    rois,
    labels
  } = await _getROIsFromToolState(naturalizedDataset, FrameOfReferenceUID);
  const managedViewer = managedViewers[0];
  for (let i = 0; i < rois.length; i++) {
    // NOTE: When saving Microscopy SR, we are attaching identifier property
    // to each ROI, and when read for display, it is coming in as "TEXT"
    // evaluation.
    // As the Dicom Microscopy Viewer will override styles for "Text" evaluations
    // to hide all other geometries, we are going to manually remove that
    // evaluation item.
    const roi = rois[i];
    const roiSymbols = Object.getOwnPropertySymbols(roi);
    const _properties = roiSymbols.find(s => s.description === 'properties');
    const properties = roi[_properties];
    properties['evaluations'] = [];
    managedViewer.addRoiGraphicWithLabel(roi, labels[i]);
  }
}
async function _getROIsFromToolState(naturalizedDataset, FrameOfReferenceUID) {
  const toolState = MeasurementReport.generateToolState(naturalizedDataset);
  const tools = Object.getOwnPropertyNames(toolState);
  const DICOMMicroscopyViewer = await __webpack_require__.e(/* import() | dicom-microscopy-viewer */ 18).then(__webpack_require__.t.bind(__webpack_require__, 42613, 23));
  const measurementGroupContentItems = _getMeasurementGroups(naturalizedDataset);
  const rois = [];
  const labels = [];
  tools.forEach(t => {
    const toolSpecificToolState = toolState[t];
    let scoord3d;
    const capsToolType = t.toUpperCase();
    const measurementGroupContentItemsForTool = measurementGroupContentItems.filter(mg => {
      const imageRegionContentItem = toArray(mg.ContentSequence).find(ci => ci.ConceptNameCodeSequence.CodeValue === dcmCodeValues.IMAGE_REGION);
      return imageRegionContentItem.GraphicType === capsToolType;
    });
    toolSpecificToolState.forEach((coordinates, index) => {
      const properties = {};
      const options = {
        coordinates,
        frameOfReferenceUID: FrameOfReferenceUID
      };
      if (t === 'Polygon') {
        scoord3d = new DICOMMicroscopyViewer.scoord3d.Polygon(options);
      } else if (t === 'Polyline') {
        scoord3d = new DICOMMicroscopyViewer.scoord3d.Polyline(options);
      } else if (t === 'Point') {
        scoord3d = new DICOMMicroscopyViewer.scoord3d.Point(options);
      } else if (t === 'Ellipse') {
        scoord3d = new DICOMMicroscopyViewer.scoord3d.Ellipse(options);
      } else {
        throw new Error('Unsupported tool type');
      }
      const measurementGroup = measurementGroupContentItemsForTool[index];
      const findingGroup = toArray(measurementGroup.ContentSequence).find(ci => ci.ConceptNameCodeSequence.CodeValue === dcmCodeValues.FINDING);
      const trackingGroup = toArray(measurementGroup.ContentSequence).find(ci => ci.ConceptNameCodeSequence.CodeValue === dcmCodeValues.TRACKING_UNIQUE_IDENTIFIER);

      /**
       * Extract presentation state from tracking identifier.
       * Currently is stored in SR but should be stored in its tags.
       */
      if (trackingGroup) {
        const regExp = /\(([^)]+)\)/;
        const matches = regExp.exec(trackingGroup.TextValue);
        if (matches && matches[1]) {
          properties.presentationState = JSON.parse(matches[1]);
          properties.marker = properties.presentationState.marker;
        }
      }
      let measurements = toArray(measurementGroup.ContentSequence).filter(ci => [dcmCodeValues.LENGTH, dcmCodeValues.AREA, dcmCodeValues.SHORT_AXIS, dcmCodeValues.LONG_AXIS, dcmCodeValues.ELLIPSE_AREA].includes(ci.ConceptNameCodeSequence.CodeValue));
      let evaluations = toArray(measurementGroup.ContentSequence).filter(ci => [dcmCodeValues.TRACKING_UNIQUE_IDENTIFIER].includes(ci.ConceptNameCodeSequence.CodeValue));

      /**
       * TODO: Resolve bug in DCMJS.
       * ConceptNameCodeSequence should be a sequence with only one item.
       */
      evaluations = evaluations.map(evaluation => {
        const e = {
          ...evaluation
        };
        e.ConceptNameCodeSequence = toArray(e.ConceptNameCodeSequence);
        return e;
      });

      /**
       * TODO: Resolve bug in DCMJS.
       * ConceptNameCodeSequence should be a sequence with only one item.
       */
      measurements = measurements.map(measurement => {
        const m = {
          ...measurement
        };
        m.ConceptNameCodeSequence = toArray(m.ConceptNameCodeSequence);
        return m;
      });
      if (measurements && measurements.length) {
        properties.measurements = measurements;
        console.log('[SR] retrieving measurements...', measurements);
      }
      if (evaluations && evaluations.length) {
        properties.evaluations = evaluations;
        console.log('[SR] retrieving evaluations...', evaluations);
      }
      const roi = new DICOMMicroscopyViewer.roi.ROI({
        scoord3d,
        properties
      });
      rois.push(roi);
      if (findingGroup) {
        labels.push(findingGroup.ConceptCodeSequence.CodeValue);
      } else {
        labels.push('');
      }
    });
  });
  return {
    rois,
    labels
  };
}
function _getMeasurementGroups(naturalizedDataset) {
  const {
    ContentSequence
  } = naturalizedDataset;
  const imagingMeasurementsContentItem = ContentSequence.find(ci => ci.ConceptNameCodeSequence.CodeValue === dcmCodeValues.IMAGING_MEASUREMENTS);
  const measurementGroupContentItems = toArray(imagingMeasurementsContentItem.ContentSequence).filter(ci => ci.ConceptNameCodeSequence.CodeValue === dcmCodeValues.MEASUREMENT_GROUP);
  return measurementGroupContentItems;
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/utils/getSourceDisplaySet.js
/**
 * Get referenced SM displaySet from SR displaySet
 *
 * @param {*} allDisplaySets
 * @param {*} microscopySRDisplaySet
 * @returns
 */
function getSourceDisplaySet(allDisplaySets, microscopySRDisplaySet) {
  const {
    ReferencedFrameOfReferenceUID
  } = microscopySRDisplaySet;
  const otherDisplaySets = allDisplaySets.filter(ds => ds.displaySetInstanceUID !== microscopySRDisplaySet.displaySetInstanceUID);
  const referencedDisplaySet = otherDisplaySets.find(displaySet => displaySet.Modality === 'SM' && (displaySet.FrameOfReferenceUID === ReferencedFrameOfReferenceUID ||
  // sometimes each depth instance has the different FrameOfReferenceID
  displaySet.othersFrameOfReferenceUID.includes(ReferencedFrameOfReferenceUID)));
  if (!referencedDisplaySet && otherDisplaySets.length >= 1) {
    console.warn('No display set with FrameOfReferenceUID', ReferencedFrameOfReferenceUID, 'single series, assuming data error, defaulting to only series.');
    return otherDisplaySets.find(displaySet => displaySet.Modality === 'SM');
  }
  return referencedDisplaySet;
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/DicomMicroscopySRSopClassHandler.js





const {
  utils: DicomMicroscopySRSopClassHandler_utils
} = core_src["default"];
const DicomMicroscopySRSopClassHandler_SOP_CLASS_UIDS = {
  COMPREHENSIVE_3D_SR: '1.2.840.10008.5.1.4.1.1.88.34'
};
const DicomMicroscopySRSopClassHandler_SOPClassHandlerId = '@ohif/extension-dicom-microscopy.sopClassHandlerModule.DicomMicroscopySRSopClassHandler';
function _getReferencedFrameOfReferenceUID(naturalizedDataset) {
  const {
    ContentSequence
  } = naturalizedDataset;
  const imagingMeasurementsContentItem = ContentSequence.find(ci => ci.ConceptNameCodeSequence.CodeValue === dcmCodeValues.IMAGING_MEASUREMENTS);
  const firstMeasurementGroupContentItem = toArray(imagingMeasurementsContentItem.ContentSequence).find(ci => ci.ConceptNameCodeSequence.CodeValue === dcmCodeValues.MEASUREMENT_GROUP);
  const imageRegionContentItem = toArray(firstMeasurementGroupContentItem.ContentSequence).find(ci => ci.ConceptNameCodeSequence.CodeValue === dcmCodeValues.IMAGE_REGION);
  return imageRegionContentItem.ReferencedFrameOfReferenceUID;
}
function DicomMicroscopySRSopClassHandler_getDisplaySetsFromSeries(instances, servicesManager, extensionManager) {
  // If the series has no instances, stop here
  if (!instances || !instances.length) {
    throw new Error('No instances were provided');
  }
  const {
    displaySetService,
    microscopyService
  } = servicesManager.services;
  const instance = instances[0];

  // TODO ! Consumption of DICOMMicroscopySRSOPClassHandler to a derived dataset or normal dataset?
  // TODO -> Easy to swap this to a "non-derived" displaySet, but unfortunately need to put it in a different extension.
  const naturalizedDataset = core_src.DicomMetadataStore.getSeries(instance.StudyInstanceUID, instance.SeriesInstanceUID).instances[0];
  const ReferencedFrameOfReferenceUID = _getReferencedFrameOfReferenceUID(naturalizedDataset);
  const {
    FrameOfReferenceUID,
    SeriesDescription,
    ContentDate,
    ContentTime,
    SeriesNumber,
    StudyInstanceUID,
    SeriesInstanceUID,
    SOPInstanceUID,
    SOPClassUID
  } = instance;
  const displaySet = {
    plugin: 'microscopy',
    Modality: 'SR',
    altImageText: 'Microscopy SR',
    displaySetInstanceUID: DicomMicroscopySRSopClassHandler_utils.guid(),
    SOPInstanceUID,
    SeriesInstanceUID,
    StudyInstanceUID,
    ReferencedFrameOfReferenceUID,
    SOPClassHandlerId: DicomMicroscopySRSopClassHandler_SOPClassHandlerId,
    SOPClassUID,
    SeriesDescription,
    // Map the content date/time to the series date/time, these are only used for filtering.
    SeriesDate: ContentDate,
    SeriesTime: ContentTime,
    SeriesNumber,
    instance,
    metadata: naturalizedDataset,
    isDerived: true,
    isLoading: false,
    isLoaded: false,
    loadError: false
  };
  displaySet.load = function (referencedDisplaySet) {
    return loadSR(microscopyService, displaySet, referencedDisplaySet).catch(error => {
      displaySet.isLoaded = false;
      displaySet.loadError = true;
      throw new Error(error);
    });
  };
  displaySet.getSourceDisplaySet = function () {
    let allDisplaySets = [];
    const studyMetadata = core_src.DicomMetadataStore.getStudy(StudyInstanceUID);
    studyMetadata.series.forEach(series => {
      const displaySets = displaySetService.getDisplaySetsForSeries(series.SeriesInstanceUID);
      allDisplaySets = allDisplaySets.concat(displaySets);
    });
    return getSourceDisplaySet(allDisplaySets, displaySet);
  };
  return [displaySet];
}
function getDicomMicroscopySRSopClassHandler(_ref) {
  let {
    servicesManager,
    extensionManager
  } = _ref;
  const getDisplaySetsFromSeries = instances => {
    return DicomMicroscopySRSopClassHandler_getDisplaySetsFromSeries(instances, servicesManager, extensionManager);
  };
  return {
    name: 'DicomMicroscopySRSopClassHandler',
    sopClassUids: [DicomMicroscopySRSopClassHandler_SOP_CLASS_UIDS.COMPREHENSIVE_3D_SR],
    getDisplaySetsFromSeries
  };
}
;// CONCATENATED MODULE: ../../../extensions/dicom-microscopy/src/index.tsx
function _extends() { _extends = Object.assign ? Object.assign.bind() : function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; }; return _extends.apply(this, arguments); }








const Component = /*#__PURE__*/react.lazy(() => {
  return Promise.all(/* import() */[__webpack_require__.e(743), __webpack_require__.e(604), __webpack_require__.e(417), __webpack_require__.e(23), __webpack_require__.e(342), __webpack_require__.e(250)]).then(__webpack_require__.bind(__webpack_require__, 76516));
});
const MicroscopyViewport = props => {
  return /*#__PURE__*/react.createElement(react.Suspense, {
    fallback: /*#__PURE__*/react.createElement("div", null, "Loading...")
  }, /*#__PURE__*/react.createElement(Component, props));
};

/**
 * You can remove any of the following modules if you don't need them.
 */
/* harmony default export */ const dicom_microscopy_src = ({
  /**
   * Only required property. Should be a unique value across all extensions.
   * You ID can be anything you want, but it should be unique.
   */
  id: id,
  async preRegistration(_ref) {
    let {
      servicesManager,
      commandsManager,
      configuration = {},
      appConfig
    } = _ref;
    servicesManager.registerService(MicroscopyService.REGISTRATION(servicesManager));
  },
  /**
   * ViewportModule should provide a list of viewports that will be available in OHIF
   * for Modes to consume and use in the viewports. Each viewport is defined by
   * {name, component} object. Example of a viewport module is the CornerstoneViewport
   * that is provided by the Cornerstone extension in OHIF.
   */
  getViewportModule(_ref2) {
    let {
      servicesManager,
      extensionManager,
      commandsManager
    } = _ref2;
    /**
     *
     * @param props {*}
     * @param props.displaySets
     * @param props.viewportId
     * @param props.viewportLabel
     * @param props.dataSource
     * @param props.viewportOptions
     * @param props.displaySetOptions
     * @returns
     */
    const ExtendedMicroscopyViewport = props => {
      const {
        viewportOptions
      } = props;
      const [viewportGrid, viewportGridService] = (0,src/* useViewportGrid */.O_)();
      const {
        activeViewportId
      } = viewportGrid;
      return /*#__PURE__*/react.createElement(MicroscopyViewport, _extends({
        servicesManager: servicesManager,
        extensionManager: extensionManager,
        commandsManager: commandsManager,
        activeViewportId: activeViewportId,
        setViewportActive: viewportId => {
          viewportGridService.setActiveViewportId(viewportId);
        },
        viewportData: viewportOptions
      }, props));
    };
    return [{
      name: 'microscopy-dicom',
      component: ExtendedMicroscopyViewport
    }];
  },
  /**
   * SopClassHandlerModule should provide a list of sop class handlers that will be
   * available in OHIF for Modes to consume and use to create displaySets from Series.
   * Each sop class handler is defined by a { name, sopClassUids, getDisplaySetsFromSeries}.
   * Examples include the default sop class handler provided by the default extension
   */
  getSopClassHandlerModule(_ref3) {
    let {
      servicesManager,
      commandsManager,
      extensionManager
    } = _ref3;
    return [getDicomMicroscopySopClassHandler({
      servicesManager,
      extensionManager
    }), getDicomMicroscopySRSopClassHandler({
      servicesManager,
      extensionManager
    })];
  },
  getPanelModule: getPanelModule,
  getCommandsModule: getCommandsModule
});

/***/ }),

/***/ 78753:
/***/ (() => {

/* (ignored) */

/***/ })

}]);