"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[686],{

/***/ 39686:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (__WEBPACK_DEFAULT_EXPORT__)
/* harmony export */ });
/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(43001);
/* harmony import */ var prop_types__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(3827);
/* harmony import */ var prop_types__WEBPACK_IMPORTED_MODULE_1___default = /*#__PURE__*/__webpack_require__.n(prop_types__WEBPACK_IMPORTED_MODULE_1__);


function OHIFCornerstoneVideoViewport(_ref) {
  let {
    displaySets
  } = _ref;
  if (displaySets && displaySets.length > 1) {
    throw new Error('OHIFCornerstoneVideoViewport: only one display set is supported for dicom video right now');
  }
  const {
    videoUrl
  } = displaySets[0];
  const mimeType = 'video/mp4';
  const [url, setUrl] = (0,react__WEBPACK_IMPORTED_MODULE_0__.useState)(null);
  (0,react__WEBPACK_IMPORTED_MODULE_0__.useEffect)(() => {
    const load = async () => {
      setUrl(await videoUrl);
    };
    load();
  }, [videoUrl]);

  // Need to copies of the source to fix a firefox bug
  return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("div", {
    className: "bg-primary-black h-full w-full"
  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("video", {
    src: url,
    controls: true,
    controlsList: "nodownload",
    preload: "auto",
    className: "h-full w-full",
    crossOrigin: "anonymous"
  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("source", {
    src: url,
    type: mimeType
  }), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("source", {
    src: url,
    type: mimeType
  }), "Video src/type not supported:", ' ', /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement("a", {
    href: url
  }, url, " of type ", mimeType)));
}
OHIFCornerstoneVideoViewport.propTypes = {
  displaySets: prop_types__WEBPACK_IMPORTED_MODULE_1___default().arrayOf((prop_types__WEBPACK_IMPORTED_MODULE_1___default().object)).isRequired
};
/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = (OHIFCornerstoneVideoViewport);

/***/ })

}]);