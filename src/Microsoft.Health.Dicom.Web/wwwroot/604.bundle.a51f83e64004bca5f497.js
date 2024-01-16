"use strict";
(self["webpackChunk"] = self["webpackChunk"] || []).push([[604],{

/***/ 97604:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   api: () => (/* binding */ api)
/* harmony export */ });
/* unused harmony exports utils, version */
function _typeof(obj) {
  "@babel/helpers - typeof";

  return _typeof = "function" == typeof Symbol && "symbol" == typeof Symbol.iterator ? function (obj) {
    return typeof obj;
  } : function (obj) {
    return obj && "function" == typeof Symbol && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj;
  }, _typeof(obj);
}
function _classCallCheck(instance, Constructor) {
  if (!(instance instanceof Constructor)) {
    throw new TypeError("Cannot call a class as a function");
  }
}
function _defineProperties(target, props) {
  for (var i = 0; i < props.length; i++) {
    var descriptor = props[i];
    descriptor.enumerable = descriptor.enumerable || false;
    descriptor.configurable = true;
    if ("value" in descriptor) descriptor.writable = true;
    Object.defineProperty(target, _toPropertyKey(descriptor.key), descriptor);
  }
}
function _createClass(Constructor, protoProps, staticProps) {
  if (protoProps) _defineProperties(Constructor.prototype, protoProps);
  if (staticProps) _defineProperties(Constructor, staticProps);
  Object.defineProperty(Constructor, "prototype", {
    writable: false
  });
  return Constructor;
}
function _toPrimitive(input, hint) {
  if (typeof input !== "object" || input === null) return input;
  var prim = input[Symbol.toPrimitive];
  if (prim !== undefined) {
    var res = prim.call(input, hint || "default");
    if (typeof res !== "object") return res;
    throw new TypeError("@@toPrimitive must return a primitive value.");
  }
  return (hint === "string" ? String : Number)(input);
}
function _toPropertyKey(arg) {
  var key = _toPrimitive(arg, "string");
  return typeof key === "symbol" ? key : String(key);
}

/**
 * Converts a Uint8Array to a String.
 * @param {Uint8Array} array that should be converted
 * @param {Number} offset array offset in case only subset of array items should
                   be extracted (default: 0)
 * @param {Number} limit maximum number of array items that should be extracted
                   (defaults to length of array)
 * @returns {String}
 */
function uint8ArrayToString(arr) {
  var offset = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 0;
  var limit = arguments.length > 2 ? arguments[2] : undefined;
  var itemLimit = limit || arr.length - offset;
  var str = '';
  for (var i = offset; i < offset + itemLimit; i++) {
    str += String.fromCharCode(arr[i]);
  }
  return str;
}

/**
 * Converts a String to a Uint8Array.
 * @param {String} str string that should be converted
 * @returns {Uint8Array}
 */
function stringToUint8Array(str) {
  var arr = new Uint8Array(str.length);
  for (var i = 0, j = str.length; i < j; i++) {
    arr[i] = str.charCodeAt(i);
  }
  return arr;
}

/**
 * Identifies the boundary in a multipart/related message header.
 * @param {String} header message header
 * @returns {String} boundary
 */
function identifyBoundary(header) {
  var parts = header.split('\r\n');
  for (var i = 0; i < parts.length; i++) {
    if (parts[i].substr(0, 2) === '--') {
      return parts[i];
    }
  }
  return null;
}

/**
 * Checks whether a given token is contained by a message at a given offset.
 * @param {Uint8Array} message message content
 * @param {Uint8Array} token substring that should be present
 * @param {Number} offset offset in message content from where search should start
 * @returns {Boolean} whether message contains token at offset
 */
function containsToken(message, token) {
  var offset = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : 0;
  if (offset + token.length > message.length) {
    return false;
  }
  var index = offset;
  for (var i = 0; i < token.length; i++) {
    if (token[i] !== message[index]) {
      return false;
    }
    index += 1;
  }
  return true;
}

/**
 * Finds a given token in a message at a given offset.
 * @param {Uint8Array} message message content
 * @param {Uint8Array} token substring that should be found
 * @param {String} offset message body offset from where search should start
 * @returns {Boolean} whether message has a part at given offset or not
 */
function findToken(message, token) {
  var offset = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : 0;
  var maxSearchLength = arguments.length > 3 ? arguments[3] : undefined;
  var searchLength = message.length;
  if (maxSearchLength) {
    searchLength = Math.min(offset + maxSearchLength, message.length);
  }
  for (var i = offset; i < searchLength; i++) {
    // If the first value of the message matches
    // the first value of the token, check if
    // this is the full token.
    if (message[i] === token[0]) {
      if (containsToken(message, token, i)) {
        return i;
      }
    }
  }
  return -1;
}

/**
 * Create a random GUID
 *
 * @return {string}
 */
function guid() {
  function s4() {
    return Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1);
  }
  return "".concat(s4() + s4(), "-").concat(s4(), "-").concat(s4(), "-").concat(s4(), "-").concat(s4()).concat(s4()).concat(s4());
}

/**
 * @typedef {Object} MultipartEncodedData
 * @property {ArrayBuffer} data The encoded Multipart Data
 * @property {String} boundary The boundary used to divide pieces of the encoded data
 */

/**
 * Encode one or more DICOM datasets into a single body so it can be
 * sent using the Multipart Content-Type.
 *
 * @param {ArrayBuffer[]} datasets Array containing each file to be encoded in the
                          multipart body, passed as ArrayBuffers.
 * @param {String} [boundary] Optional string to define a boundary between each part
                              of the multipart body. If this is not specified, a random
                              GUID will be generated.
 * @return {MultipartEncodedData} The Multipart encoded data returned as an Object. This
                                  contains both the data itself, and the boundary string
                                  used to divide it.
 */
function multipartEncode(datasets) {
  var boundary = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : guid();
  var contentType = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : 'application/dicom';
  var contentTypeString = "Content-Type: ".concat(contentType);
  var header = "\r\n--".concat(boundary, "\r\n").concat(contentTypeString, "\r\n\r\n");
  var footer = "\r\n--".concat(boundary, "--");
  var headerArray = stringToUint8Array(header);
  var footerArray = stringToUint8Array(footer);
  var headerLength = headerArray.length;
  var footerLength = footerArray.length;
  var length = 0;

  // Calculate the total length for the final array
  var contentArrays = datasets.map(function (datasetBuffer) {
    var contentArray = new Uint8Array(datasetBuffer);
    var contentLength = contentArray.length;
    length += headerLength + contentLength + footerLength;
    return contentArray;
  });

  // Allocate the array
  var multipartArray = new Uint8Array(length);

  // Set the initial header
  multipartArray.set(headerArray, 0);

  // Write each dataset into the multipart array
  var position = 0;
  contentArrays.forEach(function (contentArray) {
    multipartArray.set(headerArray, position);
    multipartArray.set(contentArray, position + headerLength);
    position += headerLength + contentArray.length;
  });
  multipartArray.set(footerArray, position);
  return {
    data: multipartArray.buffer,
    boundary: boundary
  };
}

/**
 * Decode a Multipart encoded ArrayBuffer and return the components as an Array.
 *
 * @param {ArrayBuffer} response Data encoded as a 'multipart/related' message
 * @returns {Array} The content
 */
function multipartDecode(response) {
  var message = new Uint8Array(response);

  /* Set a maximum length to search for the header boundaries, otherwise
       findToken can run for a long time
    */
  var maxSearchLength = 1000;

  // First look for the multipart mime header
  var separator = stringToUint8Array('\r\n\r\n');
  var headerIndex = findToken(message, separator, 0, maxSearchLength);
  if (headerIndex === -1) {
    throw new Error('Response message has no multipart mime header');
  }
  var header = uint8ArrayToString(message, 0, headerIndex);
  var boundaryString = identifyBoundary(header);
  if (!boundaryString) {
    throw new Error('Header of response message does not specify boundary');
  }
  var boundary = stringToUint8Array(boundaryString);
  var boundaryLength = boundary.length;
  var components = [];
  var offset = boundaryLength;

  // Loop until we cannot find any more boundaries
  var boundaryIndex;
  while (boundaryIndex !== -1) {
    // Search for the next boundary in the message, starting
    // from the current offset position
    boundaryIndex = findToken(message, boundary, offset);

    // If no further boundaries are found, stop here.
    if (boundaryIndex === -1) {
      break;
    }
    var headerTokenIndex = findToken(message, separator, offset, maxSearchLength);
    if (headerTokenIndex === -1) {
      throw new Error('Response message part has no mime header');
    }
    offset = headerTokenIndex + separator.length;

    // Extract data from response message, excluding "\r\n"
    var spacingLength = 2;
    var data = response.slice(offset, boundaryIndex - spacingLength);

    // Add the data to the array of results
    components.push(data);

    // Move the offset to the end of the current section,
    // plus the identified boundary
    offset = boundaryIndex + boundaryLength;
  }
  return components;
}

function isObject(obj) {
  return _typeof(obj) === 'object' && obj !== null;
}
function isEmptyObject(obj) {
  return Object.keys(obj).length === 0 && obj.constructor === Object;
}
function areValidRequestHooks(requestHooks) {
  var isValid = Array.isArray(requestHooks) && requestHooks.every(function (requestHook) {
    return typeof requestHook === 'function' && requestHook.length === 2;
  });
  if (!isValid) {
    console.warn('Request hooks should have the following signature: ' + 'function requestHook(request, metadata) { return request; }');
  }
  return isValid;
}
var getFirstResult = function getFirstResult(result) {
  return result[0];
};
var MEDIATYPES = {
  DICOM: 'application/dicom',
  DICOM_JSON: 'application/dicom+json',
  OCTET_STREAM: 'application/octet-stream',
  PDF: 'application/pdf',
  JPEG: 'image/jpeg',
  PNG: 'image/png'
};

/**
 * @typedef { import("../types/types").InstanceMetadata } InstanceMetadata
 */

/**
 * A callback with the request instance and metadata information
 * of the currently request being executed that should necessarily
 * return the given request optionally modified.
 * @typedef {function} RequestHook
 * @param {XMLHttpRequest} request - The original XMLHttpRequest instance.
 * @param {object} metadata - The metadata used by the request.
 */

/**
 * Class for interacting with DICOMweb RESTful services.
 */
var DICOMwebClient = /*#__PURE__*/function () {
  /**
   * @constructor
   * @param {Object} options
   * @param {String} options.url - URL of the DICOMweb RESTful Service endpoint
   * @param {String=} options.qidoURLPrefix - URL path prefix for QIDO-RS
   * @param {String=} options.wadoURLPrefix - URL path prefix for WADO-RS
   * @param {String=} options.stowURLPrefix - URL path prefix for STOW-RS
   * @param {String=} options.username - Username
   * @param {String=} options.password - Password
   * @param {Object=} options.headers - HTTP headers
   * @param {Array.<RequestHook>=} options.requestHooks - Request hooks.
   * @param {Object=} options.verbose - print to console request warnings and errors, default true
   */
  function DICOMwebClient(options) {
    _classCallCheck(this, DICOMwebClient);
    this.baseURL = options.url;
    if (!this.baseURL) {
      console.error('no DICOMweb base url provided - calls that require a URL will fail');
    }
    if ('username' in options) {
      this.username = options.username;
      if (!('password' in options)) {
        console.error('no password provided to authenticate with DICOMweb service');
      }
      this.password = options.password;
    }
    if ('qidoURLPrefix' in options) {
      console.log("use URL prefix for QIDO-RS: ".concat(options.qidoURLPrefix));
      this.qidoURL = "".concat(this.baseURL, "/").concat(options.qidoURLPrefix);
    } else {
      this.qidoURL = this.baseURL;
    }
    if ('wadoURLPrefix' in options) {
      console.log("use URL prefix for WADO-RS: ".concat(options.wadoURLPrefix));
      this.wadoURL = "".concat(this.baseURL, "/").concat(options.wadoURLPrefix);
    } else {
      this.wadoURL = this.baseURL;
    }
    if ('stowURLPrefix' in options) {
      console.log("use URL prefix for STOW-RS: ".concat(options.stowURLPrefix));
      this.stowURL = "".concat(this.baseURL, "/").concat(options.stowURLPrefix);
    } else {
      this.stowURL = this.baseURL;
    }
    if ('requestHooks' in options) {
      this.requestHooks = options.requestHooks;
    }

    // Headers to pass to requests.
    this.headers = options.headers || {};

    // Optional error interceptor callback to handle any failed request.
    this.errorInterceptor = options.errorInterceptor || function () {
      return undefined;
    };

    // Verbose - print to console request warnings and errors, default true
    this.verbose = options.verbose !== false;
  }

  /**
   * Sets verbose flag.
   *
   * @param {Boolean} verbose
   */
  _createClass(DICOMwebClient, [{
    key: "setVerbose",
    value: function setVerbose(verbose) {
      this.verbose = verbose;
    }
    /**
     * Gets verbose flag.
     *
     * @return {Boolean} verbose
     */
  }, {
    key: "getVerbose",
    value: function getVerbose() {
      return this.verbose;
    }
  }, {
    key: "_httpRequest",
    /**
     * Performs an HTTP request.
     *
     * @param {String} url
     * @param {String} method
     * @param {Object} headers
     * @param {Object} options
     * @param {Array.<RequestHook>} options.requestHooks - Request hooks.
     * @param {XMLHttpRequest} [options.request] - if specified, the request to use, otherwise one will be created; useful for adding custom upload and abort listeners/objects
     * @return {*}
     * @private
     */
    value: function _httpRequest(url, method) {
      var _this = this;
      var headers = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
      var options = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : {};
      var errorInterceptor = this.errorInterceptor,
        requestHooks = this.requestHooks;
      return new Promise(function (resolve, reject) {
        var request = options.request ? options.request : new XMLHttpRequest();
        request.open(method, url, true);
        if ('responseType' in options) {
          request.responseType = options.responseType;
        }
        if (_typeof(headers) === 'object') {
          Object.keys(headers).forEach(function (key) {
            request.setRequestHeader(key, headers[key]);
          });
        }

        // now add custom headers from the user
        // (e.g. access tokens)
        var userHeaders = _this.headers;
        Object.keys(userHeaders).forEach(function (key) {
          request.setRequestHeader(key, userHeaders[key]);
        });

        // Event triggered when upload starts
        request.onloadstart = function onloadstart() {
          // console.log('upload started: ', url)
        };

        // Event triggered when upload ends
        request.onloadend = function onloadend() {
          // console.log('upload finished')
        };

        // Handle response message
        request.onreadystatechange = function () {
          if (request.readyState === 4) {
            if (request.status === 200) {
              resolve(request.response);
            } else if (request.status === 202) {
              if (_this.verbose) {
                console.warn('some resources already existed: ', request);
              }
              resolve(request.response);
            } else if (request.status === 204) {
              if (_this.verbose) {
                console.warn('empty response for request: ', request);
              }
              resolve([]);
            } else {
              var error = new Error('request failed');
              error.request = request;
              error.response = request.response;
              error.status = request.status;
              if (_this.verbose) {
                console.error('request failed: ', request);
                console.error(error);
                console.error(error.response);
              }
              errorInterceptor(error);
              reject(error);
            }
          }
        };

        // Event triggered while download progresses
        if ('progressCallback' in options) {
          if (typeof options.progressCallback === 'function') {
            request.onprogress = options.progressCallback;
          }
        }
        if (requestHooks && areValidRequestHooks(requestHooks)) {
          var combinedHeaders = Object.assign({}, headers, _this.headers);
          var metadata = {
            method: method,
            url: url,
            headers: combinedHeaders
          };
          var pipeRequestHooks = function pipeRequestHooks(functions) {
            return function (args) {
              return functions.reduce(function (props, fn) {
                return fn(props, metadata);
              }, args);
            };
          };
          var pipedRequest = pipeRequestHooks(requestHooks);
          request = pipedRequest(request);
        }

        // Add withCredentials to request if needed
        if ('withCredentials' in options) {
          if (options.withCredentials) {
            request.withCredentials = true;
          }
        }
        if ('data' in options) {
          request.send(options.data);
        } else {
          request.send();
        }
      });
    }
    /**
     * Performs an HTTP GET request.
     *
     * @param {String} url
     * @param {Object} headers
     * @param {Object} responseType
     * @param {Function} progressCallback
     * @return {*}
     * @private
     */
  }, {
    key: "_httpGet",
    value: function _httpGet(url, headers, responseType, progressCallback, withCredentials) {
      return this._httpRequest(url, 'get', headers, {
        responseType: responseType,
        progressCallback: progressCallback,
        withCredentials: withCredentials
      });
    }
    /**
     * Performs an HTTP GET request that accepts a message with application/json
     * media type.
     *
     * @param {String} url
     * @param {Object} params
     * @param {Function} progressCallback
     * @return {*}
     * @private
     */
  }, {
    key: "_httpGetApplicationJson",
    value: function _httpGetApplicationJson(url) {
      var params = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
      var progressCallback = arguments.length > 2 ? arguments[2] : undefined;
      var withCredentials = arguments.length > 3 ? arguments[3] : undefined;
      var urlWithQueryParams = url;
      if (_typeof(params) === 'object') {
        if (!isEmptyObject(params)) {
          urlWithQueryParams += DICOMwebClient._parseQueryParameters(params);
        }
      }
      var headers = {
        Accept: MEDIATYPES.DICOM_JSON
      };
      var responseType = 'json';
      return this._httpGet(urlWithQueryParams, headers, responseType, progressCallback, withCredentials);
    }
    /**
     * Performs an HTTP GET request that accepts a message with application/pdf
     * media type.
     *
     * @param {String} url
     * @param {Object} params
     * @param {Function} progressCallback
     * @return {*}
     * @private
     */
  }, {
    key: "_httpGetApplicationPdf",
    value: function _httpGetApplicationPdf(url) {
      var params = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
      var progressCallback = arguments.length > 2 ? arguments[2] : undefined;
      var withCredentials = arguments.length > 3 ? arguments[3] : undefined;
      var urlWithQueryParams = url;
      if (_typeof(params) === 'object') {
        if (!isEmptyObject(params)) {
          urlWithQueryParams += DICOMwebClient._parseQueryParameters(params);
        }
      }
      var headers = {
        Accept: MEDIATYPES.PDF
      };
      var responseType = 'json';
      return this._httpGet(urlWithQueryParams, headers, responseType, progressCallback, withCredentials);
    }
    /**
     * Performs an HTTP GET request that accepts a message with an image
     media type.
     *
     * @param {String} url
     * @param {Object[]} mediaTypes
     * @param {Object} params
     * @param {Function} progressCallback
     * @return {*}
     * @private
     */
  }, {
    key: "_httpGetImage",
    value: function _httpGetImage(url, mediaTypes) {
      var params = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
      var progressCallback = arguments.length > 3 ? arguments[3] : undefined;
      var withCredentials = arguments.length > 4 ? arguments[4] : undefined;
      var urlWithQueryParams = url;
      if (_typeof(params) === 'object') {
        if (!isEmptyObject(params)) {
          urlWithQueryParams += DICOMwebClient._parseQueryParameters(params);
        }
      }
      var supportedMediaTypes = ['image/', 'image/*', 'image/jpeg', 'image/jp2', 'image/gif', 'image/png'];
      var acceptHeaderFieldValue = DICOMwebClient._buildAcceptHeaderFieldValue(mediaTypes, supportedMediaTypes);
      var headers = {
        Accept: acceptHeaderFieldValue
      };
      var responseType = 'arraybuffer';
      return this._httpGet(urlWithQueryParams, headers, responseType, progressCallback, withCredentials);
    }
    /**
     * Performs an HTTP GET request that accepts a message with a text
     media type.
     *
     * @param {String} url
     * @param {Object[]} mediaTypes
     * @param {Object} params
     * @param {Function} progressCallback
     * @return {*}
     * @private
     */
  }, {
    key: "_httpGetText",
    value: function _httpGetText(url, mediaTypes) {
      var params = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
      var progressCallback = arguments.length > 3 ? arguments[3] : undefined;
      var withCredentials = arguments.length > 4 ? arguments[4] : undefined;
      var urlWithQueryParams = url;
      if (_typeof(params) === 'object') {
        if (!isEmptyObject(params)) {
          urlWithQueryParams += DICOMwebClient._parseQueryParameters(params);
        }
      }
      var supportedMediaTypes = ['text/', 'text/*', 'text/html', 'text/plain', 'text/rtf', 'text/xml'];
      var acceptHeaderFieldValue = DICOMwebClient._buildAcceptHeaderFieldValue(mediaTypes, supportedMediaTypes);
      var headers = {
        Accept: acceptHeaderFieldValue
      };
      var responseType = 'arraybuffer';
      return this._httpGet(urlWithQueryParams, headers, responseType, progressCallback, withCredentials);
    }
    /**
     * Performs an HTTP GET request that accepts a message with a video
     media type.
     *
     * @param {String} url
     * @param {Object[]} mediaTypes
     * @param {Object} params
     * @param {Function} progressCallback
     * @return {*}
     * @private
     */
  }, {
    key: "_httpGetVideo",
    value: function _httpGetVideo(url, mediaTypes) {
      var params = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
      var progressCallback = arguments.length > 3 ? arguments[3] : undefined;
      var withCredentials = arguments.length > 4 ? arguments[4] : undefined;
      var urlWithQueryParams = url;
      if (_typeof(params) === 'object') {
        if (!isEmptyObject(params)) {
          urlWithQueryParams += DICOMwebClient._parseQueryParameters(params);
        }
      }
      var supportedMediaTypes = ['video/', 'video/*', 'video/mpeg', 'video/mp4', 'video/H265'];
      var acceptHeaderFieldValue = DICOMwebClient._buildAcceptHeaderFieldValue(mediaTypes, supportedMediaTypes);
      var headers = {
        Accept: acceptHeaderFieldValue
      };
      var responseType = 'arraybuffer';
      return this._httpGet(urlWithQueryParams, headers, responseType, progressCallback, withCredentials);
    }
    /**
     * Asserts that a given media type is valid.
     *
     * @params {String} mediaType media type
     */
  }, {
    key: "_httpGetMultipartImage",
    /**
     * Performs an HTTP GET request that accepts a multipart message with an image media type.
     *
     * @param {String} url - Unique resource locator
     * @param {Object[]} mediaTypes - Acceptable media types and optionally the UIDs of the
     corresponding transfer syntaxes
     * @param {Array} byteRange - Start and end of byte range
     * @param {Object} params - Additional HTTP GET query parameters
     * @param {Boolean} rendered - Whether resource should be requested using rendered media types
     * @param {Function} progressCallback
     * @private
     * @returns {Promise<Array>} Content of HTTP message body parts
     */
    value: function _httpGetMultipartImage(url, mediaTypes, byteRange, params) {
      var rendered = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : false;
      var progressCallback = arguments.length > 5 ? arguments[5] : undefined;
      var withCredentials = arguments.length > 6 ? arguments[6] : undefined;
      var headers = {};
      var supportedMediaTypes;
      if (rendered) {
        supportedMediaTypes = ['image/jpeg', 'image/gif', 'image/png', 'image/jp2'];
      } else {
        supportedMediaTypes = {
          '1.2.840.10008.1.2.5': ['image/x-dicom-rle'],
          '1.2.840.10008.1.2.4.50': ['image/jpeg'],
          '1.2.840.10008.1.2.4.51': ['image/jpeg'],
          '1.2.840.10008.1.2.4.57': ['image/jpeg'],
          '1.2.840.10008.1.2.4.70': ['image/jpeg'],
          '1.2.840.10008.1.2.4.80': ['image/x-jls', 'image/jls'],
          '1.2.840.10008.1.2.4.81': ['image/x-jls', 'image/jls'],
          '1.2.840.10008.1.2.4.90': ['image/jp2'],
          '1.2.840.10008.1.2.4.91': ['image/jp2'],
          '1.2.840.10008.1.2.4.92': ['image/jpx'],
          '1.2.840.10008.1.2.4.93': ['image/jpx']
        };
        if (byteRange) {
          headers.Range = DICOMwebClient._buildRangeHeaderFieldValue(byteRange);
        }
      }
      headers.Accept = DICOMwebClient._buildMultipartAcceptHeaderFieldValue(mediaTypes, supportedMediaTypes);
      return this._httpGet(url, headers, 'arraybuffer', progressCallback, withCredentials).then(multipartDecode);
    }
    /**
     * Performs an HTTP GET request that accepts a multipart message with a video media type.
     *
     * @param {String} url - Unique resource locator
     * @param {Object[]} mediaTypes - Acceptable media types and optionally the UIDs of the
     corresponding transfer syntaxes
     * @param {Array} byteRange - Start and end of byte range
     * @param {Object} params - Additional HTTP GET query parameters
     * @param {Boolean} rendered - Whether resource should be requested using rendered media types
     * @param {Function} progressCallback
     * @private
     * @returns {Promise<Array>} Content of HTTP message body parts
     */
  }, {
    key: "_httpGetMultipartVideo",
    value: function _httpGetMultipartVideo(url, mediaTypes, byteRange, params) {
      var rendered = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : false;
      var progressCallback = arguments.length > 5 ? arguments[5] : undefined;
      var withCredentials = arguments.length > 6 ? arguments[6] : undefined;
      var headers = {};
      var supportedMediaTypes;
      if (rendered) {
        supportedMediaTypes = ['video/', 'video/*', 'video/mpeg2', 'video/mp4', 'video/H265'];
      } else {
        supportedMediaTypes = {
          '1.2.840.10008.1.2.4.100': ['video/mpeg2'],
          '1.2.840.10008.1.2.4.101': ['video/mpeg2'],
          '1.2.840.10008.1.2.4.102': ['video/mp4'],
          '1.2.840.10008.1.2.4.103': ['video/mp4'],
          '1.2.840.10008.1.2.4.104': ['video/mp4'],
          '1.2.840.10008.1.2.4.105': ['video/mp4'],
          '1.2.840.10008.1.2.4.106': ['video/mp4']
        };
        if (byteRange) {
          headers.Range = DICOMwebClient._buildRangeHeaderFieldValue(byteRange);
        }
      }
      headers.Accept = DICOMwebClient._buildMultipartAcceptHeaderFieldValue(mediaTypes, supportedMediaTypes);
      return this._httpGet(url, headers, 'arraybuffer', progressCallback, withCredentials).then(multipartDecode);
    }
    /**
     * Performs an HTTP GET request that accepts a multipart message
     * with a application/dicom media type.
     *
     * @param {String} url - Unique resource locator
     * @param {Object[]} mediaTypes - Acceptable media types and optionally the UIDs of the
     corresponding transfer syntaxes
     * @param {Object} params - Additional HTTP GET query parameters
     * @param {Function} progressCallback
     * @private
     * @returns {Promise<Array>} Content of HTTP message body parts
     */
  }, {
    key: "_httpGetMultipartApplicationDicom",
    value: function _httpGetMultipartApplicationDicom(url, mediaTypes, params, progressCallback, withCredentials) {
      var headers = {};
      var defaultMediaType = 'application/dicom';
      var supportedMediaTypes = {
        '1.2.840.10008.1.2.1': [defaultMediaType],
        '1.2.840.10008.1.2.5': [defaultMediaType],
        '1.2.840.10008.1.2.4.50': [defaultMediaType],
        '1.2.840.10008.1.2.4.51': [defaultMediaType],
        '1.2.840.10008.1.2.4.57': [defaultMediaType],
        '1.2.840.10008.1.2.4.70': [defaultMediaType],
        '1.2.840.10008.1.2.4.80': [defaultMediaType],
        '1.2.840.10008.1.2.4.81': [defaultMediaType],
        '1.2.840.10008.1.2.4.90': [defaultMediaType],
        '1.2.840.10008.1.2.4.91': [defaultMediaType],
        '1.2.840.10008.1.2.4.92': [defaultMediaType],
        '1.2.840.10008.1.2.4.93': [defaultMediaType],
        '1.2.840.10008.1.2.4.100': [defaultMediaType],
        '1.2.840.10008.1.2.4.101': [defaultMediaType],
        '1.2.840.10008.1.2.4.102': [defaultMediaType],
        '1.2.840.10008.1.2.4.103': [defaultMediaType],
        '1.2.840.10008.1.2.4.104': [defaultMediaType],
        '1.2.840.10008.1.2.4.105': [defaultMediaType],
        '1.2.840.10008.1.2.4.106': [defaultMediaType]
      };
      var acceptableMediaTypes = mediaTypes;
      if (!mediaTypes) {
        acceptableMediaTypes = [{
          mediaType: defaultMediaType
        }];
      }
      headers.Accept = DICOMwebClient._buildMultipartAcceptHeaderFieldValue(acceptableMediaTypes, supportedMediaTypes);
      return this._httpGet(url, headers, 'arraybuffer', progressCallback, withCredentials).then(multipartDecode);
    }
    /**
     * Performs an HTTP GET request that accepts a multipart message
     * with a application/octet-stream media type.
     *
     * @param {String} url - Unique resource locator
     * @param {Object[]} mediaTypes - Acceptable media types and optionally the UIDs of the
     corresponding transfer syntaxes
     * @param {Array} byteRange start and end of byte range
     * @param {Object} params - Additional HTTP GET query parameters
     * @param {Function} progressCallback
     * @private
     * @returns {Promise<Array>} Content of HTTP message body parts
     */
  }, {
    key: "_httpGetMultipartApplicationOctetStream",
    value: function _httpGetMultipartApplicationOctetStream(url, mediaTypes, byteRange, params, progressCallback, withCredentials) {
      var headers = {};
      var defaultMediaType = 'application/octet-stream';
      var supportedMediaTypes = {
        '1.2.840.10008.1.2.1': [defaultMediaType]
      };
      var acceptableMediaTypes = mediaTypes;
      if (!mediaTypes) {
        acceptableMediaTypes = [{
          mediaType: defaultMediaType
        }];
      }
      if (byteRange) {
        headers.Range = DICOMwebClient._buildRangeHeaderFieldValue(byteRange);
      }
      headers.Accept = DICOMwebClient._buildMultipartAcceptHeaderFieldValue(acceptableMediaTypes, supportedMediaTypes);
      return this._httpGet(url, headers, 'arraybuffer', progressCallback, withCredentials).then(multipartDecode);
    }
    /**
     * Performs an HTTP POST request.
     *
     * @param {String} url - Unique resource locator
     * @param {Object} headers - HTTP header fields
     * @param {Array} data - Data that should be stored
     * @param {Function} progressCallback
     * @param {Function} progressCallback
     * @param {XMLHttpRequest} request - if specified, the request to use, otherwise one will be created; useful for adding custom upload and abort listeners/objects
     * @private
     * @returns {Promise} Response
     */
  }, {
    key: "_httpPost",
    value: function _httpPost(url, headers, data, progressCallback, withCredentials, request) {
      return this._httpRequest(url, 'post', headers, {
        data: data,
        progressCallback: progressCallback,
        withCredentials: withCredentials,
        request: request
      });
    }
    /**
     * Performs an HTTP POST request with content-type application/dicom+json.
     *
     * @param {String} url - Unique resource locator
     * @param {Object} headers - HTTP header fields
     * @param {Array} data - Data that should be stored
     * @param {Function} progressCallback
     * @private
     * @returns {Promise} Response
     */
  }, {
    key: "_httpPostApplicationJson",
    value: function _httpPostApplicationJson(url, data, progressCallback, withCredentials) {
      var headers = {
        'Content-Type': MEDIATYPES.DICOM_JSON
      };
      return this._httpPost(url, headers, data, progressCallback, withCredentials);
    }
    /**
     * Parses media type and extracts its type and subtype.
     *
     * @param {String} mediaType - HTTP media type (e.g. image/jpeg)
     * @private
     * @returns {String[]} Media type and subtype
     */
  }, {
    key: "searchForStudies",
    /**
     * Searches for DICOM studies.
     *
     * @param {Object} options
     * @param {Object} [options.queryParams] - HTTP query parameters
     * @return {Object[]} Study representations (http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_6.7.html#table_6.7.1-2)
     */
    value: function searchForStudies() {
      var options = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
      console.log('search for studies');
      var withCredentials = false;
      var url = "".concat(this.qidoURL, "/studies");
      if ('queryParams' in options) {
        url += DICOMwebClient._parseQueryParameters(options.queryParams);
      }
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      return this._httpGetApplicationJson(url, {}, false, withCredentials);
    }
    /**
     * Retrieves metadata for a DICOM study.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @returns {Promise<InstanceMetadata[]>} Metadata elements in DICOM JSON format for each instance
                        belonging to the study
     */
  }, {
    key: "retrieveStudyMetadata",
    value: function retrieveStudyMetadata(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required for retrieval of study metadata');
      }
      console.log("retrieve metadata of study ".concat(options.studyInstanceUID));
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/metadata");
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      return this._httpGetApplicationJson(url, {}, false, withCredentials);
    }
    /**
     * Searches for DICOM series.
     *
     * @param {Object} options
     * @param {Object} [options.studyInstanceUID] - Study Instance UID
     * @param {Object} [options.queryParams] - HTTP query parameters
     * @returns {Object[]} Series representations (http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_6.7.html#table_6.7.1-2a)
     */
  }, {
    key: "searchForSeries",
    value: function searchForSeries() {
      var options = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
      var url = this.qidoURL;
      if ('studyInstanceUID' in options) {
        console.log("search series of study ".concat(options.studyInstanceUID));
        url += "/studies/".concat(options.studyInstanceUID);
      }
      url += '/series';
      if ('queryParams' in options) {
        url += DICOMwebClient._parseQueryParameters(options.queryParams);
      }
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      return this._httpGetApplicationJson(url, {}, false, withCredentials);
    }
    /**
     * Retrieves metadata for a DICOM series.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @returns {Promise<InstanceMetadata[]>} Metadata elements in DICOM JSON format for each instance
                        belonging to the series
     */
  }, {
    key: "retrieveSeriesMetadata",
    value: function retrieveSeriesMetadata(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required for retrieval of series metadata');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required for retrieval of series metadata');
      }
      console.log("retrieve metadata of series ".concat(options.seriesInstanceUID));
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID, "/metadata");
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      return this._httpGetApplicationJson(url, {}, false, withCredentials);
    }
    /**
     * Searches for DICOM Instances.
     *
     * @param {Object} options
     * @param {String} [options.studyInstanceUID] - Study Instance UID
     * @param {String} [options.seriesInstanceUID] - Series Instance UID
     * @param {Object} [options.queryParams] - HTTP query parameters
     * @returns {Object[]} Instance representations (http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_6.7.html#table_6.7.1-2b)
     */
  }, {
    key: "searchForInstances",
    value: function searchForInstances() {
      var options = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
      var url = this.qidoURL;
      var withCredentials = false;
      if ('studyInstanceUID' in options) {
        url += "/studies/".concat(options.studyInstanceUID);
        if ('seriesInstanceUID' in options) {
          console.log("search for instances of series ".concat(options.seriesInstanceUID));
          url += "/series/".concat(options.seriesInstanceUID);
        } else {
          console.log("search for instances of study ".concat(options.studyInstanceUID));
        }
      } else {
        console.log('search for instances');
      }
      url += '/instances';
      if ('queryParams' in options) {
        url += DICOMwebClient._parseQueryParameters(options.queryParams);
      }
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      return this._httpGetApplicationJson(url, {}, false, withCredentials);
    }
    /** Returns a WADO-URI URL for an instance
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @returns {String} WADO-URI URL
     */
  }, {
    key: "buildInstanceWadoURIUrl",
    value: function buildInstanceWadoURIUrl(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required.');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required.');
      }
      if (!('sopInstanceUID' in options)) {
        throw new Error('SOP Instance UID is required.');
      }
      var contentType = options.contentType || MEDIATYPES.DICOM;
      var transferSyntax = options.transferSyntax || '*';
      var params = [];
      params.push('requestType=WADO');
      params.push("studyUID=".concat(options.studyInstanceUID));
      params.push("seriesUID=".concat(options.seriesInstanceUID));
      params.push("objectUID=".concat(options.sopInstanceUID));
      params.push("contentType=".concat(contentType));
      params.push("transferSyntax=".concat(transferSyntax));
      var paramString = params.join('&');
      return "".concat(this.wadoURL, "?").concat(paramString);
    }
    /**
     * Retrieves metadata for a DICOM Instance.
     *
     * @param {Object} options object
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @returns {Promise<InstanceMetadata>} metadata elements in DICOM JSON format
     */
  }, {
    key: "retrieveInstanceMetadata",
    value: function retrieveInstanceMetadata(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required for retrieval of instance metadata');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required for retrieval of instance metadata');
      }
      if (!('sopInstanceUID' in options)) {
        throw new Error('SOP Instance UID is required for retrieval of instance metadata');
      }
      console.log("retrieve metadata of instance ".concat(options.sopInstanceUID));
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID, "/instances/").concat(options.sopInstanceUID, "/metadata");
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      return this._httpGetApplicationJson(url, {}, false, withCredentials);
    }
    /**
     * Retrieves frames for a DICOM Instance.
     * @param {Object} options options object
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @param {String} options.frameNumbers - One-based indices of Frame Items
     * @returns {Array} frame items as byte arrays of the pixel data element
     */
  }, {
    key: "retrieveInstanceFrames",
    value: function retrieveInstanceFrames(options) {
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
      console.log("retrieve frames ".concat(options.frameNumbers.toString(), " of instance ").concat(options.sopInstanceUID));
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID, "/instances/").concat(options.sopInstanceUID, "/frames/").concat(options.frameNumbers.toString());
      var mediaTypes = options.mediaTypes;
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      var progressCallback = false;
      if ('progressCallback' in options) {
        progressCallback = options.progressCallback;
      }
      if (!mediaTypes) {
        return this._httpGetMultipartApplicationOctetStream(url, false, false, false, progressCallback, withCredentials);
      }
      var sharedMediaTypes = DICOMwebClient._getSharedMediaTypes(mediaTypes);
      if (sharedMediaTypes.length > 1) {
        /**
         * Enable request of frames that are stored either compressed
         * (image/* media type) or uncompressed (application/octet-stream
         * media type).
         */
        var supportedMediaTypes = {
          '1.2.840.10008.1.2.1': ['application/octet-stream'],
          '1.2.840.10008.1.2.5': ['image/x-dicom-rle'],
          '1.2.840.10008.1.2.4.50': ['image/jpeg'],
          '1.2.840.10008.1.2.4.51': ['image/jpeg'],
          '1.2.840.10008.1.2.4.57': ['image/jpeg'],
          '1.2.840.10008.1.2.4.70': ['image/jpeg'],
          '1.2.840.10008.1.2.4.80': ['image/x-jls', 'image/jls'],
          '1.2.840.10008.1.2.4.81': ['image/x-jls', 'image/jls'],
          '1.2.840.10008.1.2.4.90': ['image/jp2'],
          '1.2.840.10008.1.2.4.91': ['image/jp2'],
          '1.2.840.10008.1.2.4.92': ['image/jpx'],
          '1.2.840.10008.1.2.4.93': ['image/jpx']
        };
        var headers = {
          Accept: DICOMwebClient._buildMultipartAcceptHeaderFieldValue(mediaTypes, supportedMediaTypes)
        };
        return this._httpGet(url, headers, 'arraybuffer', progressCallback, withCredentials).then(multipartDecode);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType.startsWith('application')) {
        return this._httpGetMultipartApplicationOctetStream(url, mediaTypes, false, false, progressCallback, withCredentials);
      }
      if (commonMediaType.startsWith('image')) {
        return this._httpGetMultipartImage(url, mediaTypes, false, false, false, progressCallback, withCredentials);
      }
      if (commonMediaType.startsWith('video')) {
        return this._httpGetMultipartVideo(url, mediaTypes, false, false, false, progressCallback, withCredentials);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported for retrieval of frames."));
    }
    /**
     * Retrieves an individual, server-side rendered DICOM Instance.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @param {String[]} [options.mediaType] - Acceptable HTTP media types
     * @param {Object} [options.queryParams] - HTTP query parameters
     * @returns {Promise<ArrayBuffer>} Rendered DICOM Instance
     */
  }, {
    key: "retrieveInstanceRendered",
    value: function retrieveInstanceRendered(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required for retrieval of rendered instance');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required for retrieval of rendered instance');
      }
      if (!('sopInstanceUID' in options)) {
        throw new Error('SOP Instance UID is required for retrieval of rendered instance');
      }
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID, "/instances/").concat(options.sopInstanceUID, "/rendered");
      var mediaTypes = options.mediaTypes,
        queryParams = options.queryParams;
      var headers = {};
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      var progressCallback = false;
      if ('progressCallback' in options) {
        progressCallback = options.progressCallback;
      }
      if (!mediaTypes) {
        var responseType = 'arraybuffer';
        if (queryParams) {
          url += DICOMwebClient._parseQueryParameters(queryParams);
        }
        return this._httpGet(url, headers, responseType, progressCallback, withCredentials);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType.startsWith('image')) {
        return this._httpGetImage(url, mediaTypes, queryParams, progressCallback, withCredentials);
      }
      if (commonMediaType.startsWith('video')) {
        return this._httpGetVideo(url, mediaTypes, queryParams, progressCallback, withCredentials);
      }
      if (commonMediaType.startsWith('text')) {
        return this._httpGetText(url, mediaTypes, queryParams, progressCallback, withCredentials);
      }
      if (commonMediaType === MEDIATYPES.PDF) {
        return this._httpGetApplicationPdf(url, queryParams, progressCallback, withCredentials);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported ") + 'for retrieval of rendered instance.');
    }
    /**
     * Retrieves a thumbnail of an DICOM Instance.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @param {String[]} [options.mediaType] - Acceptable HTTP media types
     * @param {Object} [options.queryParams] - HTTP query parameters
     * @returns {ArrayBuffer} Thumbnail
     */
  }, {
    key: "retrieveInstanceThumbnail",
    value: function retrieveInstanceThumbnail(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required for retrieval of rendered instance');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required for retrieval of rendered instance');
      }
      if (!('sopInstanceUID' in options)) {
        throw new Error('SOP Instance UID is required for retrieval of rendered instance');
      }
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID, "/instances/").concat(options.sopInstanceUID, "/thumbnail");
      var mediaTypes = options.mediaTypes,
        queryParams = options.queryParams;
      var headers = {};
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      var progressCallback = false;
      if ('progressCallback' in options) {
        progressCallback = options.progressCallback;
      }
      if (!mediaTypes) {
        var responseType = 'arraybuffer';
        if (queryParams) {
          url += DICOMwebClient._parseQueryParameters(queryParams);
        }
        return this._httpGet(url, headers, responseType, progressCallback, withCredentials);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType.startsWith('image')) {
        return this._httpGetImage(url, mediaTypes, queryParams, progressCallback, withCredentials);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported ") + 'for retrieval of rendered instance.');
    }
    /**
     * Retrieves rendered frames for a DICOM Instance.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @param {String} options.frameNumbers - One-based indices of Frame Items
     * @param {String[]} [options.mediaType] - Acceptable HTTP media types
     * @param {Object} [options.queryParams] - HTTP query parameters
     * @returns {ArrayBuffer[]} Rendered Frame Items as byte arrays
     */
  }, {
    key: "retrieveInstanceFramesRendered",
    value: function retrieveInstanceFramesRendered(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required for retrieval of rendered instance frames');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required for retrieval of rendered instance frames');
      }
      if (!('sopInstanceUID' in options)) {
        throw new Error('SOP Instance UID is required for retrieval of rendered instance frames');
      }
      if (!('frameNumbers' in options)) {
        throw new Error('frame numbers are required for retrieval of rendered instance frames');
      }
      console.debug("retrieve rendered frames ".concat(options.frameNumbers.toString(), " of instance ").concat(options.sopInstanceUID));
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID, "/instances/").concat(options.sopInstanceUID, "/frames/").concat(options.frameNumbers.toString(), "/rendered");
      var mediaTypes = options.mediaTypes,
        queryParams = options.queryParams;
      var headers = {};
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      var progressCallback = false;
      if ('progressCallback' in options) {
        progressCallback = options.progressCallback;
      }
      if (!mediaTypes) {
        var responseType = 'arraybuffer';
        if (queryParams) {
          url += DICOMwebClient._parseQueryParameters(queryParams);
        }
        return this._httpGet(url, headers, responseType, false, withCredentials);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType.startsWith('image')) {
        return this._httpGetImage(url, mediaTypes, queryParams, progressCallback, withCredentials);
      }
      if (commonMediaType.startsWith('video')) {
        return this._httpGetVideo(url, mediaTypes, queryParams, progressCallback, withCredentials);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported ") + 'for retrieval of rendered frame.');
    }
    /**
     * Retrieves thumbnail of frames for a DICOM Instance.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @param {String} options.frameNumbers - One-based indices of Frame Items
     * @param {Object} [options.queryParams] - HTTP query parameters
     * @returns {ArrayBuffer[]} Rendered Frame Items as byte arrays
     */
  }, {
    key: "retrieveInstanceFramesThumbnail",
    value: function retrieveInstanceFramesThumbnail(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required for retrieval of rendered instance frames');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required for retrieval of rendered instance frames');
      }
      if (!('sopInstanceUID' in options)) {
        throw new Error('SOP Instance UID is required for retrieval of rendered instance frames');
      }
      if (!('frameNumbers' in options)) {
        throw new Error('frame numbers are required for retrieval of rendered instance frames');
      }
      console.debug("retrieve rendered frames ".concat(options.frameNumbers.toString(), " of instance ").concat(options.sopInstanceUID));
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID, "/instances/").concat(options.sopInstanceUID, "/frames/").concat(options.frameNumbers.toString(), "/thumbnail");
      var mediaTypes = options.mediaTypes,
        queryParams = options.queryParams;
      var headers = {};
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      var progressCallback = false;
      if ('progressCallback' in options) {
        progressCallback = options.progressCallback;
      }
      if (!mediaTypes) {
        var responseType = 'arraybuffer';
        if (queryParams) {
          url += DICOMwebClient._parseQueryParameters(queryParams);
        }
        return this._httpGet(url, headers, responseType, progressCallback, withCredentials);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType.startsWith('image')) {
        return this._httpGetImage(url, mediaTypes, queryParams, progressCallback, withCredentials);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported ") + 'for retrieval of rendered frame.');
    }
    /**
     * Retrieves a DICOM Instance.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {String} options.sopInstanceUID - SOP Instance UID
     * @returns {Promise<ArrayBuffer>} DICOM Part 10 file as Arraybuffer
     */
  }, {
    key: "retrieveInstance",
    value: function retrieveInstance(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required');
      }
      if (!('sopInstanceUID' in options)) {
        throw new Error('SOP Instance UID is required');
      }
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID, "/instances/").concat(options.sopInstanceUID);
      var mediaTypes = options.mediaTypes;
      var _options$withCredenti = options.withCredentials,
        withCredentials = _options$withCredenti === void 0 ? false : _options$withCredenti;
      var _options$progressCall = options.progressCallback,
        progressCallback = _options$progressCall === void 0 ? false : _options$progressCall;
      if (!mediaTypes) {
        return this._httpGetMultipartApplicationDicom(url, false, false, progressCallback, withCredentials).then(getFirstResult);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType === MEDIATYPES.DICOM) {
        return this._httpGetMultipartApplicationDicom(url, mediaTypes, false, progressCallback, withCredentials).then(getFirstResult);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported for retrieval of instance."));
    }
    /**
     * Retrieves all DICOM Instances of a Series.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @param {String} options.seriesInstanceUID - Series Instance UID
     * @param {Function} options.progressCallback
     * @returns {Promise<ArrayBuffer[]>} DICOM Instances
     */
  }, {
    key: "retrieveSeries",
    value: function retrieveSeries(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required');
      }
      if (!('seriesInstanceUID' in options)) {
        throw new Error('Series Instance UID is required');
      }
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID, "/series/").concat(options.seriesInstanceUID);
      var mediaTypes = options.mediaTypes;
      var withCredentials = false;
      if ('withCredentials' in options) {
        if (options.withCredentials) {
          withCredentials = options.withCredentials;
        }
      }
      var progressCallback = false;
      if ('progressCallback' in options) {
        progressCallback = options.progressCallback;
      }
      if (!mediaTypes) {
        return this._httpGetMultipartApplicationDicom(url, false, false, progressCallback, withCredentials);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType === MEDIATYPES.DICOM) {
        return this._httpGetMultipartApplicationDicom(url, mediaTypes, false, progressCallback, withCredentials);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported for retrieval of series."));
    }
    /**
     * Retrieves all DICOM Instances of a Study.
     *
     * @param {Object} options
     * @param {String} options.studyInstanceUID - Study Instance UID
     * @returns {ArrayBuffer[]} DICOM Instances
     */
  }, {
    key: "retrieveStudy",
    value: function retrieveStudy(options) {
      if (!('studyInstanceUID' in options)) {
        throw new Error('Study Instance UID is required');
      }
      var url = "".concat(this.wadoURL, "/studies/").concat(options.studyInstanceUID);
      var mediaTypes = options.mediaTypes;
      var _options$withCredenti2 = options.withCredentials,
        withCredentials = _options$withCredenti2 === void 0 ? false : _options$withCredenti2;
      var _options$progressCall2 = options.progressCallback,
        progressCallback = _options$progressCall2 === void 0 ? false : _options$progressCall2;
      if (!mediaTypes) {
        return this._httpGetMultipartApplicationDicom(url, false, false, progressCallback, withCredentials);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType === MEDIATYPES.DICOM) {
        return this._httpGetMultipartApplicationDicom(url, mediaTypes, false, progressCallback, withCredentials);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported for retrieval of study."));
    }
    /**
     * Retrieves and parses BulkData from a BulkDataURI location.
     * Decodes the multipart encoded data and returns the resulting data
     * as an ArrayBuffer.
     *
     * See http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_6.5.5.html
     *
     * @param {Object} options
     * @param {String} BulkDataURI - URI for retrieval of bulkdata
     * @returns {Promise<Array>} Bulkdata parts
     */
  }, {
    key: "retrieveBulkData",
    value: function retrieveBulkData(options) {
      if (!('BulkDataURI' in options)) {
        throw new Error('BulkDataURI is required.');
      }
      var url = options.BulkDataURI;
      var mediaTypes = options.mediaTypes,
        byteRange = options.byteRange;
      var _options$withCredenti3 = options.withCredentials,
        withCredentials = _options$withCredenti3 === void 0 ? false : _options$withCredenti3;
      var _options$progressCall3 = options.progressCallback,
        progressCallback = _options$progressCall3 === void 0 ? false : _options$progressCall3;
      if (!mediaTypes) {
        return this._httpGetMultipartApplicationOctetStream(url, mediaTypes, byteRange, false, false, withCredentials);
      }
      var commonMediaType = DICOMwebClient._getCommonMediaType(mediaTypes);
      if (commonMediaType === MEDIATYPES.OCTET_STREAM) {
        return this._httpGetMultipartApplicationOctetStream(url, mediaTypes, byteRange, false, progressCallback, withCredentials);
      }
      if (commonMediaType.startsWith('image')) {
        return this._httpGetMultipartImage(url, mediaTypes, byteRange, false, false, progressCallback, withCredentials);
      }
      throw new Error("Media type ".concat(commonMediaType, " is not supported for retrieval of bulk data."));
    }
    /**
     * Stores DICOM Instances.
     *
     * @param {Object} options
     * @param {ArrayBuffer[]} options.datasets - DICOM Instances in PS3.10 format
     * @param {String} [options.studyInstanceUID] - Study Instance UID
     * @param {XMLHttpRequest} [options.request] - if specified, the request to use, otherwise one will be created; useful for adding custom upload and abort listeners/objects
     * @returns {Promise} Response message
     */
  }, {
    key: "storeInstances",
    value: function storeInstances(options) {
      if (!('datasets' in options)) {
        throw new Error('datasets are required for storing');
      }
      var url = "".concat(this.stowURL, "/studies");
      if ('studyInstanceUID' in options) {
        url += "/".concat(options.studyInstanceUID);
      }
      var _multipartEncode = multipartEncode(options.datasets),
        data = _multipartEncode.data,
        boundary = _multipartEncode.boundary;
      var headers = {
        'Content-Type': "multipart/related; type=\"application/dicom\"; boundary=\"".concat(boundary, "\"")
      };
      var _options$withCredenti4 = options.withCredentials,
        withCredentials = _options$withCredenti4 === void 0 ? false : _options$withCredenti4;
      return this._httpPost(url, headers, data, options.progressCallback, withCredentials, options.request);
    }
  }], [{
    key: "_parseQueryParameters",
    value: function _parseQueryParameters() {
      var params = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
      var queryString = '?';
      Object.keys(params).forEach(function (key, index) {
        if (index !== 0) {
          queryString += '&';
        }
        queryString += "".concat(key, "=").concat(encodeURIComponent(params[key]));
      });
      return queryString;
    }
  }, {
    key: "_assertMediaTypeIsValid",
    value: function _assertMediaTypeIsValid(mediaType) {
      if (!mediaType) {
        throw new Error("Not a valid media type: ".concat(mediaType));
      }
      var sepIndex = mediaType.indexOf('/');
      if (sepIndex === -1) {
        throw new Error("Not a valid media type: ".concat(mediaType));
      }
      var mediaTypeType = mediaType.slice(0, sepIndex);
      var types = ['application', 'image', 'text', 'video'];
      if (!types.includes(mediaTypeType)) {
        throw new Error("Not a valid media type: ".concat(mediaType));
      }
      if (mediaType.slice(sepIndex + 1).includes('/')) {
        throw new Error("Not a valid media type: ".concat(mediaType));
      }
    }
  }, {
    key: "_parseMediaType",
    value: function _parseMediaType(mediaType) {
      DICOMwebClient._assertMediaTypeIsValid(mediaType);
      return mediaType.split('/');
    }
    /**
     * Builds an accept header field value for HTTP GET request messages.
     *
     * @param {Object[]} mediaTypes - Acceptable media types
     * @param {Object[]} supportedMediaTypes - Supported media types
     * @return {*}
     * @private
     */
  }, {
    key: "_buildAcceptHeaderFieldValue",
    value: function _buildAcceptHeaderFieldValue(mediaTypes, supportedMediaTypes) {
      if (!Array.isArray(mediaTypes)) {
        throw new Error('Acceptable media types must be provided as an Array');
      }
      var fieldValueParts = mediaTypes.map(function (item) {
        var mediaType = item.mediaType;
        DICOMwebClient._assertMediaTypeIsValid(mediaType);
        if (!supportedMediaTypes.includes(mediaType)) {
          throw new Error("Media type ".concat(mediaType, " is not supported for requested resource"));
        }
        return mediaType;
      });
      return fieldValueParts.join(', ');
    }
    /**
       * Builds an accept header field value for HTTP GET multipart request
       messages.
       *
       * @param {Object[]} mediaTypes - Acceptable media types
       * @param {Object[]} supportedMediaTypes - Supported media types
       * @private
       */
  }, {
    key: "_buildMultipartAcceptHeaderFieldValue",
    value: function _buildMultipartAcceptHeaderFieldValue(mediaTypes, supportedMediaTypes) {
      if (!Array.isArray(mediaTypes)) {
        throw new Error('Acceptable media types must be provided as an Array');
      }
      if (!Array.isArray(supportedMediaTypes) && !isObject(supportedMediaTypes)) {
        throw new Error('Supported media types must be provided as an Array or an Object');
      }
      var fieldValueParts = [];
      mediaTypes.forEach(function (item) {
        var transferSyntaxUID = item.transferSyntaxUID,
          mediaType = item.mediaType;
        DICOMwebClient._assertMediaTypeIsValid(mediaType);
        var fieldValue = "multipart/related; type=\"".concat(mediaType, "\"");
        if (isObject(supportedMediaTypes)) {
          // SupportedMediaTypes is a lookup table that maps Transfer Syntax UID
          // to one or more Media Types
          if (!Object.values(supportedMediaTypes).flat(1).includes(mediaType)) {
            if (!mediaType.endsWith('/*') || !mediaType.endsWith('/')) {
              throw new Error("Media type ".concat(mediaType, " is not supported for requested resource"));
            }
          }
          if (transferSyntaxUID) {
            if (transferSyntaxUID !== '*') {
              if (!Object.keys(supportedMediaTypes).includes(transferSyntaxUID)) {
                throw new Error("Transfer syntax ".concat(transferSyntaxUID, " is not supported for requested resource"));
              }
              var expectedMediaTypes = supportedMediaTypes[transferSyntaxUID];
              if (!expectedMediaTypes.includes(mediaType)) {
                var actualType = DICOMwebClient._parseMediaType(mediaType)[0];
                expectedMediaTypes.map(function (expectedMediaType) {
                  var expectedType = DICOMwebClient._parseMediaType(expectedMediaType)[0];
                  var haveSameType = actualType === expectedType;
                  if (haveSameType && (mediaType.endsWith('/*') || mediaType.endsWith('/'))) {
                    return;
                  }
                  throw new Error("Transfer syntax ".concat(transferSyntaxUID, " is not supported for requested resource"));
                });
              }
            }
            fieldValue += "; transfer-syntax=".concat(transferSyntaxUID);
          }
        } else if (Array.isArray(supportedMediaTypes) && !supportedMediaTypes.includes(mediaType)) {
          throw new Error("Media type ".concat(mediaType, " is not supported for requested resource"));
        }
        fieldValueParts.push(fieldValue);
      });
      return fieldValueParts.join(', ');
    }
    /**
     * Builds a range header field value for HTTP GET request messages.
     *
     * @param {Array} byteRange - Start and end of byte range
     * @returns {String} Range header field value
     * @private
     */
  }, {
    key: "_buildRangeHeaderFieldValue",
    value: function _buildRangeHeaderFieldValue() {
      var byteRange = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : [];
      if (byteRange.length === 1) {
        return "bytes=".concat(byteRange[0], "-");
      }
      if (byteRange.length === 2) {
        return "bytes=".concat(byteRange[0], "-").concat(byteRange[1]);
      }
      return 'bytes=0-';
    }
    /**
     * Gets types that are shared among acceptable media types.
     *
     * @param {Object[]} mediaTypes - Acceptable media types and optionally the UIDs of the
     corresponding transfer syntaxes
     * @private
     * @returns {String[]} Types that are shared among acceptable media types
     */
  }, {
    key: "_getSharedMediaTypes",
    value: function _getSharedMediaTypes(mediaTypes) {
      var types = new Set();
      if (!mediaTypes || !mediaTypes.length) {
        return types;
      }
      mediaTypes.forEach(function (item) {
        var mediaType = item.mediaType;
        var type = DICOMwebClient._parseMediaType(mediaType)[0];
        types.add("".concat(type, "/"));
      });
      return Array.from(types);
    }
    /**
     * Gets common type of acceptable media types and asserts that only
     one type is specified. For example, ``("image/jpeg", "image/jp2")``
     will pass, but ``("image/jpeg", "video/mpeg2")`` will raise an
     exception.
     *
     * @param {Object[]} mediaTypes - Acceptable media types and optionally the UIDs of the
     corresponding transfer syntaxes
     * @private
     * @returns {String[]} Common media type
     */
  }, {
    key: "_getCommonMediaType",
    value: function _getCommonMediaType(mediaTypes) {
      if (!mediaTypes || !mediaTypes.length) {
        throw new Error('No acceptable media types provided');
      }
      var sharedMediaTypes = DICOMwebClient._getSharedMediaTypes(mediaTypes);
      if (sharedMediaTypes.length === 0) {
        throw new Error('No common acceptable media type could be identified.');
      } else if (sharedMediaTypes.length > 1) {
        throw new Error('Acceptable media types must have the same type.');
      }
      return sharedMediaTypes[0];
    }
  }]);
  return DICOMwebClient;
}();

function findSubstring(str, before, after) {
  var beforeIndex = str.lastIndexOf(before) + before.length;
  if (beforeIndex < before.length) {
    return null;
  }
  if (after !== undefined) {
    var afterIndex = str.lastIndexOf(after);
    if (afterIndex < 0) {
      return null;
    }
    return str.substring(beforeIndex, afterIndex);
  }
  return str.substring(beforeIndex);
}
function getStudyInstanceUIDFromUri(uri) {
  var uid = findSubstring(uri, 'studies/', '/series');
  if (!uid) {
    uid = findSubstring(uri, 'studies/');
  }
  if (!uid) {
    console.debug("Study Instance UID could not be dertermined from URI \"".concat(uri, "\""));
  }
  return uid;
}
function getSeriesInstanceUIDFromUri(uri) {
  var uid = findSubstring(uri, 'series/', '/instances');
  if (!uid) {
    uid = findSubstring(uri, 'series/');
  }
  if (!uid) {
    console.debug("Series Instance UID could not be dertermined from URI \"".concat(uri, "\""));
  }
  return uid;
}
function getSOPInstanceUIDFromUri(uri) {
  var uid = findSubstring(uri, '/instances/', '/frames');
  if (!uid) {
    uid = findSubstring(uri, '/instances/', '/metadata');
  }
  if (!uid) {
    uid = findSubstring(uri, '/instances/');
  }
  if (!uid) {
    console.debug("SOP Instance UID could not be dertermined from URI\"".concat(uri, "\""));
  }
  return uid;
}
function getFrameNumbersFromUri(uri) {
  var numbers = findSubstring(uri, '/frames/', '/rendered');
  if (!numbers) {
    numbers = findSubstring(uri, '/frames/');
  }
  if (numbers === undefined) {
    console.debug("Frame Numbers could not be dertermined from URI\"".concat(uri, "\""));
  }
  return numbers.split(',');
}

var version = '0.5.2';

var api = {
  DICOMwebClient: DICOMwebClient
};
var utils = {
  getStudyInstanceUIDFromUri: getStudyInstanceUIDFromUri,
  getSeriesInstanceUIDFromUri: getSeriesInstanceUIDFromUri,
  getSOPInstanceUIDFromUri: getSOPInstanceUIDFromUri,
  getFrameNumbersFromUri: getFrameNumbersFromUri
};




/***/ })

}]);