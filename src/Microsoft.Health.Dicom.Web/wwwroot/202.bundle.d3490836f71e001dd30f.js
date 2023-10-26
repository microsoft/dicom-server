(self["webpackChunk"] = self["webpackChunk"] || []).push([[202],{

/***/ 91202:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

"use strict";
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   Y: () => (/* binding */ index),
/* harmony export */   adaptersRT: () => (/* binding */ adaptersRT),
/* harmony export */   adaptersSEG: () => (/* binding */ adaptersSEG),
/* harmony export */   adaptersSR: () => (/* binding */ adaptersSR),
/* harmony export */   helpers: () => (/* binding */ index$1)
/* harmony export */ });
/* harmony import */ var dcmjs__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(67540);
/* harmony import */ var buffer__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(58955);
/* harmony import */ var ndarray__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(87513);
/* harmony import */ var ndarray__WEBPACK_IMPORTED_MODULE_2___default = /*#__PURE__*/__webpack_require__.n(ndarray__WEBPACK_IMPORTED_MODULE_2__);
/* harmony import */ var lodash_clonedeep__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(11677);
/* harmony import */ var lodash_clonedeep__WEBPACK_IMPORTED_MODULE_3___default = /*#__PURE__*/__webpack_require__.n(lodash_clonedeep__WEBPACK_IMPORTED_MODULE_3__);
/* harmony import */ var gl_matrix__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(45451);






function _iterableToArrayLimit(arr, i) {
  var _i = null == arr ? null : "undefined" != typeof Symbol && arr[Symbol.iterator] || arr["@@iterator"];
  if (null != _i) {
    var _s,
      _e,
      _x,
      _r,
      _arr = [],
      _n = !0,
      _d = !1;
    try {
      if (_x = (_i = _i.call(arr)).next, 0 === i) {
        if (Object(_i) !== _i) return;
        _n = !1;
      } else for (; !(_n = (_s = _x.call(_i)).done) && (_arr.push(_s.value), _arr.length !== i); _n = !0);
    } catch (err) {
      _d = !0, _e = err;
    } finally {
      try {
        if (!_n && null != _i.return && (_r = _i.return(), Object(_r) !== _r)) return;
      } finally {
        if (_d) throw _e;
      }
    }
    return _arr;
  }
}
function ownKeys(object, enumerableOnly) {
  var keys = Object.keys(object);
  if (Object.getOwnPropertySymbols) {
    var symbols = Object.getOwnPropertySymbols(object);
    enumerableOnly && (symbols = symbols.filter(function (sym) {
      return Object.getOwnPropertyDescriptor(object, sym).enumerable;
    })), keys.push.apply(keys, symbols);
  }
  return keys;
}
function _objectSpread2(target) {
  for (var i = 1; i < arguments.length; i++) {
    var source = null != arguments[i] ? arguments[i] : {};
    i % 2 ? ownKeys(Object(source), !0).forEach(function (key) {
      _defineProperty(target, key, source[key]);
    }) : Object.getOwnPropertyDescriptors ? Object.defineProperties(target, Object.getOwnPropertyDescriptors(source)) : ownKeys(Object(source)).forEach(function (key) {
      Object.defineProperty(target, key, Object.getOwnPropertyDescriptor(source, key));
    });
  }
  return target;
}
function _regeneratorRuntime() {
  _regeneratorRuntime = function () {
    return exports;
  };
  var exports = {},
    Op = Object.prototype,
    hasOwn = Op.hasOwnProperty,
    defineProperty = Object.defineProperty || function (obj, key, desc) {
      obj[key] = desc.value;
    },
    $Symbol = "function" == typeof Symbol ? Symbol : {},
    iteratorSymbol = $Symbol.iterator || "@@iterator",
    asyncIteratorSymbol = $Symbol.asyncIterator || "@@asyncIterator",
    toStringTagSymbol = $Symbol.toStringTag || "@@toStringTag";
  function define(obj, key, value) {
    return Object.defineProperty(obj, key, {
      value: value,
      enumerable: !0,
      configurable: !0,
      writable: !0
    }), obj[key];
  }
  try {
    define({}, "");
  } catch (err) {
    define = function (obj, key, value) {
      return obj[key] = value;
    };
  }
  function wrap(innerFn, outerFn, self, tryLocsList) {
    var protoGenerator = outerFn && outerFn.prototype instanceof Generator ? outerFn : Generator,
      generator = Object.create(protoGenerator.prototype),
      context = new Context(tryLocsList || []);
    return defineProperty(generator, "_invoke", {
      value: makeInvokeMethod(innerFn, self, context)
    }), generator;
  }
  function tryCatch(fn, obj, arg) {
    try {
      return {
        type: "normal",
        arg: fn.call(obj, arg)
      };
    } catch (err) {
      return {
        type: "throw",
        arg: err
      };
    }
  }
  exports.wrap = wrap;
  var ContinueSentinel = {};
  function Generator() {}
  function GeneratorFunction() {}
  function GeneratorFunctionPrototype() {}
  var IteratorPrototype = {};
  define(IteratorPrototype, iteratorSymbol, function () {
    return this;
  });
  var getProto = Object.getPrototypeOf,
    NativeIteratorPrototype = getProto && getProto(getProto(values([])));
  NativeIteratorPrototype && NativeIteratorPrototype !== Op && hasOwn.call(NativeIteratorPrototype, iteratorSymbol) && (IteratorPrototype = NativeIteratorPrototype);
  var Gp = GeneratorFunctionPrototype.prototype = Generator.prototype = Object.create(IteratorPrototype);
  function defineIteratorMethods(prototype) {
    ["next", "throw", "return"].forEach(function (method) {
      define(prototype, method, function (arg) {
        return this._invoke(method, arg);
      });
    });
  }
  function AsyncIterator(generator, PromiseImpl) {
    function invoke(method, arg, resolve, reject) {
      var record = tryCatch(generator[method], generator, arg);
      if ("throw" !== record.type) {
        var result = record.arg,
          value = result.value;
        return value && "object" == typeof value && hasOwn.call(value, "__await") ? PromiseImpl.resolve(value.__await).then(function (value) {
          invoke("next", value, resolve, reject);
        }, function (err) {
          invoke("throw", err, resolve, reject);
        }) : PromiseImpl.resolve(value).then(function (unwrapped) {
          result.value = unwrapped, resolve(result);
        }, function (error) {
          return invoke("throw", error, resolve, reject);
        });
      }
      reject(record.arg);
    }
    var previousPromise;
    defineProperty(this, "_invoke", {
      value: function (method, arg) {
        function callInvokeWithMethodAndArg() {
          return new PromiseImpl(function (resolve, reject) {
            invoke(method, arg, resolve, reject);
          });
        }
        return previousPromise = previousPromise ? previousPromise.then(callInvokeWithMethodAndArg, callInvokeWithMethodAndArg) : callInvokeWithMethodAndArg();
      }
    });
  }
  function makeInvokeMethod(innerFn, self, context) {
    var state = "suspendedStart";
    return function (method, arg) {
      if ("executing" === state) throw new Error("Generator is already running");
      if ("completed" === state) {
        if ("throw" === method) throw arg;
        return doneResult();
      }
      for (context.method = method, context.arg = arg;;) {
        var delegate = context.delegate;
        if (delegate) {
          var delegateResult = maybeInvokeDelegate(delegate, context);
          if (delegateResult) {
            if (delegateResult === ContinueSentinel) continue;
            return delegateResult;
          }
        }
        if ("next" === context.method) context.sent = context._sent = context.arg;else if ("throw" === context.method) {
          if ("suspendedStart" === state) throw state = "completed", context.arg;
          context.dispatchException(context.arg);
        } else "return" === context.method && context.abrupt("return", context.arg);
        state = "executing";
        var record = tryCatch(innerFn, self, context);
        if ("normal" === record.type) {
          if (state = context.done ? "completed" : "suspendedYield", record.arg === ContinueSentinel) continue;
          return {
            value: record.arg,
            done: context.done
          };
        }
        "throw" === record.type && (state = "completed", context.method = "throw", context.arg = record.arg);
      }
    };
  }
  function maybeInvokeDelegate(delegate, context) {
    var methodName = context.method,
      method = delegate.iterator[methodName];
    if (undefined === method) return context.delegate = null, "throw" === methodName && delegate.iterator.return && (context.method = "return", context.arg = undefined, maybeInvokeDelegate(delegate, context), "throw" === context.method) || "return" !== methodName && (context.method = "throw", context.arg = new TypeError("The iterator does not provide a '" + methodName + "' method")), ContinueSentinel;
    var record = tryCatch(method, delegate.iterator, context.arg);
    if ("throw" === record.type) return context.method = "throw", context.arg = record.arg, context.delegate = null, ContinueSentinel;
    var info = record.arg;
    return info ? info.done ? (context[delegate.resultName] = info.value, context.next = delegate.nextLoc, "return" !== context.method && (context.method = "next", context.arg = undefined), context.delegate = null, ContinueSentinel) : info : (context.method = "throw", context.arg = new TypeError("iterator result is not an object"), context.delegate = null, ContinueSentinel);
  }
  function pushTryEntry(locs) {
    var entry = {
      tryLoc: locs[0]
    };
    1 in locs && (entry.catchLoc = locs[1]), 2 in locs && (entry.finallyLoc = locs[2], entry.afterLoc = locs[3]), this.tryEntries.push(entry);
  }
  function resetTryEntry(entry) {
    var record = entry.completion || {};
    record.type = "normal", delete record.arg, entry.completion = record;
  }
  function Context(tryLocsList) {
    this.tryEntries = [{
      tryLoc: "root"
    }], tryLocsList.forEach(pushTryEntry, this), this.reset(!0);
  }
  function values(iterable) {
    if (iterable) {
      var iteratorMethod = iterable[iteratorSymbol];
      if (iteratorMethod) return iteratorMethod.call(iterable);
      if ("function" == typeof iterable.next) return iterable;
      if (!isNaN(iterable.length)) {
        var i = -1,
          next = function next() {
            for (; ++i < iterable.length;) if (hasOwn.call(iterable, i)) return next.value = iterable[i], next.done = !1, next;
            return next.value = undefined, next.done = !0, next;
          };
        return next.next = next;
      }
    }
    return {
      next: doneResult
    };
  }
  function doneResult() {
    return {
      value: undefined,
      done: !0
    };
  }
  return GeneratorFunction.prototype = GeneratorFunctionPrototype, defineProperty(Gp, "constructor", {
    value: GeneratorFunctionPrototype,
    configurable: !0
  }), defineProperty(GeneratorFunctionPrototype, "constructor", {
    value: GeneratorFunction,
    configurable: !0
  }), GeneratorFunction.displayName = define(GeneratorFunctionPrototype, toStringTagSymbol, "GeneratorFunction"), exports.isGeneratorFunction = function (genFun) {
    var ctor = "function" == typeof genFun && genFun.constructor;
    return !!ctor && (ctor === GeneratorFunction || "GeneratorFunction" === (ctor.displayName || ctor.name));
  }, exports.mark = function (genFun) {
    return Object.setPrototypeOf ? Object.setPrototypeOf(genFun, GeneratorFunctionPrototype) : (genFun.__proto__ = GeneratorFunctionPrototype, define(genFun, toStringTagSymbol, "GeneratorFunction")), genFun.prototype = Object.create(Gp), genFun;
  }, exports.awrap = function (arg) {
    return {
      __await: arg
    };
  }, defineIteratorMethods(AsyncIterator.prototype), define(AsyncIterator.prototype, asyncIteratorSymbol, function () {
    return this;
  }), exports.AsyncIterator = AsyncIterator, exports.async = function (innerFn, outerFn, self, tryLocsList, PromiseImpl) {
    void 0 === PromiseImpl && (PromiseImpl = Promise);
    var iter = new AsyncIterator(wrap(innerFn, outerFn, self, tryLocsList), PromiseImpl);
    return exports.isGeneratorFunction(outerFn) ? iter : iter.next().then(function (result) {
      return result.done ? result.value : iter.next();
    });
  }, defineIteratorMethods(Gp), define(Gp, toStringTagSymbol, "Generator"), define(Gp, iteratorSymbol, function () {
    return this;
  }), define(Gp, "toString", function () {
    return "[object Generator]";
  }), exports.keys = function (val) {
    var object = Object(val),
      keys = [];
    for (var key in object) keys.push(key);
    return keys.reverse(), function next() {
      for (; keys.length;) {
        var key = keys.pop();
        if (key in object) return next.value = key, next.done = !1, next;
      }
      return next.done = !0, next;
    };
  }, exports.values = values, Context.prototype = {
    constructor: Context,
    reset: function (skipTempReset) {
      if (this.prev = 0, this.next = 0, this.sent = this._sent = undefined, this.done = !1, this.delegate = null, this.method = "next", this.arg = undefined, this.tryEntries.forEach(resetTryEntry), !skipTempReset) for (var name in this) "t" === name.charAt(0) && hasOwn.call(this, name) && !isNaN(+name.slice(1)) && (this[name] = undefined);
    },
    stop: function () {
      this.done = !0;
      var rootRecord = this.tryEntries[0].completion;
      if ("throw" === rootRecord.type) throw rootRecord.arg;
      return this.rval;
    },
    dispatchException: function (exception) {
      if (this.done) throw exception;
      var context = this;
      function handle(loc, caught) {
        return record.type = "throw", record.arg = exception, context.next = loc, caught && (context.method = "next", context.arg = undefined), !!caught;
      }
      for (var i = this.tryEntries.length - 1; i >= 0; --i) {
        var entry = this.tryEntries[i],
          record = entry.completion;
        if ("root" === entry.tryLoc) return handle("end");
        if (entry.tryLoc <= this.prev) {
          var hasCatch = hasOwn.call(entry, "catchLoc"),
            hasFinally = hasOwn.call(entry, "finallyLoc");
          if (hasCatch && hasFinally) {
            if (this.prev < entry.catchLoc) return handle(entry.catchLoc, !0);
            if (this.prev < entry.finallyLoc) return handle(entry.finallyLoc);
          } else if (hasCatch) {
            if (this.prev < entry.catchLoc) return handle(entry.catchLoc, !0);
          } else {
            if (!hasFinally) throw new Error("try statement without catch or finally");
            if (this.prev < entry.finallyLoc) return handle(entry.finallyLoc);
          }
        }
      }
    },
    abrupt: function (type, arg) {
      for (var i = this.tryEntries.length - 1; i >= 0; --i) {
        var entry = this.tryEntries[i];
        if (entry.tryLoc <= this.prev && hasOwn.call(entry, "finallyLoc") && this.prev < entry.finallyLoc) {
          var finallyEntry = entry;
          break;
        }
      }
      finallyEntry && ("break" === type || "continue" === type) && finallyEntry.tryLoc <= arg && arg <= finallyEntry.finallyLoc && (finallyEntry = null);
      var record = finallyEntry ? finallyEntry.completion : {};
      return record.type = type, record.arg = arg, finallyEntry ? (this.method = "next", this.next = finallyEntry.finallyLoc, ContinueSentinel) : this.complete(record);
    },
    complete: function (record, afterLoc) {
      if ("throw" === record.type) throw record.arg;
      return "break" === record.type || "continue" === record.type ? this.next = record.arg : "return" === record.type ? (this.rval = this.arg = record.arg, this.method = "return", this.next = "end") : "normal" === record.type && afterLoc && (this.next = afterLoc), ContinueSentinel;
    },
    finish: function (finallyLoc) {
      for (var i = this.tryEntries.length - 1; i >= 0; --i) {
        var entry = this.tryEntries[i];
        if (entry.finallyLoc === finallyLoc) return this.complete(entry.completion, entry.afterLoc), resetTryEntry(entry), ContinueSentinel;
      }
    },
    catch: function (tryLoc) {
      for (var i = this.tryEntries.length - 1; i >= 0; --i) {
        var entry = this.tryEntries[i];
        if (entry.tryLoc === tryLoc) {
          var record = entry.completion;
          if ("throw" === record.type) {
            var thrown = record.arg;
            resetTryEntry(entry);
          }
          return thrown;
        }
      }
      throw new Error("illegal catch attempt");
    },
    delegateYield: function (iterable, resultName, nextLoc) {
      return this.delegate = {
        iterator: values(iterable),
        resultName: resultName,
        nextLoc: nextLoc
      }, "next" === this.method && (this.arg = undefined), ContinueSentinel;
    }
  }, exports;
}
function asyncGeneratorStep(gen, resolve, reject, _next, _throw, key, arg) {
  try {
    var info = gen[key](arg);
    var value = info.value;
  } catch (error) {
    reject(error);
    return;
  }
  if (info.done) {
    resolve(value);
  } else {
    Promise.resolve(value).then(_next, _throw);
  }
}
function _asyncToGenerator(fn) {
  return function () {
    var self = this,
      args = arguments;
    return new Promise(function (resolve, reject) {
      var gen = fn.apply(self, args);
      function _next(value) {
        asyncGeneratorStep(gen, resolve, reject, _next, _throw, "next", value);
      }
      function _throw(err) {
        asyncGeneratorStep(gen, resolve, reject, _next, _throw, "throw", err);
      }
      _next(undefined);
    });
  };
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
function _defineProperty(obj, key, value) {
  key = _toPropertyKey(key);
  if (key in obj) {
    Object.defineProperty(obj, key, {
      value: value,
      enumerable: true,
      configurable: true,
      writable: true
    });
  } else {
    obj[key] = value;
  }
  return obj;
}
function _slicedToArray(arr, i) {
  return _arrayWithHoles(arr) || _iterableToArrayLimit(arr, i) || _unsupportedIterableToArray(arr, i) || _nonIterableRest();
}
function _toConsumableArray(arr) {
  return _arrayWithoutHoles(arr) || _iterableToArray(arr) || _unsupportedIterableToArray(arr) || _nonIterableSpread();
}
function _arrayWithoutHoles(arr) {
  if (Array.isArray(arr)) return _arrayLikeToArray(arr);
}
function _arrayWithHoles(arr) {
  if (Array.isArray(arr)) return arr;
}
function _iterableToArray(iter) {
  if (typeof Symbol !== "undefined" && iter[Symbol.iterator] != null || iter["@@iterator"] != null) return Array.from(iter);
}
function _unsupportedIterableToArray(o, minLen) {
  if (!o) return;
  if (typeof o === "string") return _arrayLikeToArray(o, minLen);
  var n = Object.prototype.toString.call(o).slice(8, -1);
  if (n === "Object" && o.constructor) n = o.constructor.name;
  if (n === "Map" || n === "Set") return Array.from(o);
  if (n === "Arguments" || /^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(n)) return _arrayLikeToArray(o, minLen);
}
function _arrayLikeToArray(arr, len) {
  if (len == null || len > arr.length) len = arr.length;
  for (var i = 0, arr2 = new Array(len); i < len; i++) arr2[i] = arr[i];
  return arr2;
}
function _nonIterableSpread() {
  throw new TypeError("Invalid attempt to spread non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.");
}
function _nonIterableRest() {
  throw new TypeError("Invalid attempt to destructure non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.");
}
function _createForOfIteratorHelper(o, allowArrayLike) {
  var it = typeof Symbol !== "undefined" && o[Symbol.iterator] || o["@@iterator"];
  if (!it) {
    if (Array.isArray(o) || (it = _unsupportedIterableToArray(o)) || allowArrayLike && o && typeof o.length === "number") {
      if (it) o = it;
      var i = 0;
      var F = function () {};
      return {
        s: F,
        n: function () {
          if (i >= o.length) return {
            done: true
          };
          return {
            done: false,
            value: o[i++]
          };
        },
        e: function (e) {
          throw e;
        },
        f: F
      };
    }
    throw new TypeError("Invalid attempt to iterate non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.");
  }
  var normalCompletion = true,
    didErr = false,
    err;
  return {
    s: function () {
      it = it.call(o);
    },
    n: function () {
      var step = it.next();
      normalCompletion = step.done;
      return step;
    },
    e: function (e) {
      didErr = true;
      err = e;
    },
    f: function () {
      try {
        if (!normalCompletion && it.return != null) it.return();
      } finally {
        if (didErr) throw err;
      }
    }
  };
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

var toArray = function (x) { return (Array.isArray(x) ? x : [x]); };

/**
 * Returns a function that checks if a given content item's ConceptNameCodeSequence.CodeMeaning
 * matches the provided codeMeaningName.
 * @param codeMeaningName - The CodeMeaning to match against.
 * @returns A function that takes a content item and returns a boolean indicating whether the
 * content item's CodeMeaning matches the provided codeMeaningName.
 */
var codeMeaningEquals = function (codeMeaningName) {
    return function (contentItem) {
        return (contentItem.ConceptNameCodeSequence.CodeMeaning === codeMeaningName);
    };
};

/**
 * Checks if a given content item's GraphicType property matches a specified value.
 * @param {string} graphicType - The value to compare the content item's GraphicType property to.
 * @returns {function} A function that takes a content item and returns a boolean indicating whether its GraphicType property matches the specified value.
 */
var graphicTypeEquals = function (graphicType) {
    return function (contentItem) {
        return contentItem && contentItem.GraphicType === graphicType;
    };
};

var datasetToDict = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .data */ .aT.datasetToDict;
/**
 * Trigger file download from an array buffer
 * @param bufferOrDataset - ArrayBuffer or DicomDataset
 * @param filename - name of the file to download
 */
function downloadDICOMData(bufferOrDataset, filename) {
    var blob;
    if (bufferOrDataset instanceof ArrayBuffer) {
        blob = new Blob([bufferOrDataset], { type: "application/dicom" });
    }
    else {
        if (!bufferOrDataset._meta) {
            throw new Error("Dataset must have a _meta property");
        }
        var buffer = buffer__WEBPACK_IMPORTED_MODULE_1__/* .Buffer */ .lW.from(datasetToDict(bufferOrDataset).write());
        blob = new Blob([buffer], { type: "application/dicom" });
    }
    var link = document.createElement("a");
    link.href = window.URL.createObjectURL(blob);
    link.download = filename;
    link.click();
}

var index$1 = /*#__PURE__*/Object.freeze({
  __proto__: null,
  codeMeaningEquals: codeMeaningEquals,
  downloadDICOMData: downloadDICOMData,
  graphicTypeEquals: graphicTypeEquals,
  toArray: toArray
});

var TID1500$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID1500,
  addAccessors$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.addAccessors;
var StructuredReport$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .derivations */ .U7.StructuredReport;
var Normalizer$4 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .normalizers */ .oq.Normalizer;
var TID1500MeasurementReport$1 = TID1500$1.TID1500MeasurementReport,
  TID1501MeasurementGroup$1 = TID1500$1.TID1501MeasurementGroup;
var DicomMetaDictionary$4 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .data */ .aT.DicomMetaDictionary;
var FINDING$2 = {
  CodingSchemeDesignator: "DCM",
  CodeValue: "121071"
};
var FINDING_SITE$2 = {
  CodingSchemeDesignator: "SCT",
  CodeValue: "363698007"
};
var FINDING_SITE_OLD$1 = {
  CodingSchemeDesignator: "SRT",
  CodeValue: "G-C0E3"
};
var codeValueMatch$1 = function codeValueMatch(group, code, oldCode) {
  var ConceptNameCodeSequence = group.ConceptNameCodeSequence;
  if (!ConceptNameCodeSequence) return;
  var CodingSchemeDesignator = ConceptNameCodeSequence.CodingSchemeDesignator,
    CodeValue = ConceptNameCodeSequence.CodeValue;
  return CodingSchemeDesignator == code.CodingSchemeDesignator && CodeValue == code.CodeValue || oldCode && CodingSchemeDesignator == oldCode.CodingSchemeDesignator && CodeValue == oldCode.CodeValue;
};
function getTID300ContentItem$1(tool, toolType, ReferencedSOPSequence, toolClass) {
  var args = toolClass.getTID300RepresentationArguments(tool);
  args.ReferencedSOPSequence = ReferencedSOPSequence;
  var TID300Measurement = new toolClass.TID300Representation(args);
  return TID300Measurement;
}
function getMeasurementGroup$1(toolType, toolData, ReferencedSOPSequence) {
  var toolTypeData = toolData[toolType];
  var toolClass = MeasurementReport$1.CORNERSTONE_TOOL_CLASSES_BY_TOOL_TYPE[toolType];
  if (!toolTypeData || !toolTypeData.data || !toolTypeData.data.length || !toolClass) {
    return;
  }

  // Loop through the array of tool instances
  // for this tool
  var Measurements = toolTypeData.data.map(function (tool) {
    return getTID300ContentItem$1(tool, toolType, ReferencedSOPSequence, toolClass);
  });
  return new TID1501MeasurementGroup$1(Measurements);
}
var MeasurementReport$1 = /*#__PURE__*/function () {
  function MeasurementReport() {
    _classCallCheck(this, MeasurementReport);
  }
  _createClass(MeasurementReport, null, [{
    key: "getSetupMeasurementData",
    value: function getSetupMeasurementData(MeasurementGroup) {
      var ContentSequence = MeasurementGroup.ContentSequence;
      var contentSequenceArr = toArray(ContentSequence);
      var findingGroup = contentSequenceArr.find(function (group) {
        return codeValueMatch$1(group, FINDING$2);
      });
      var findingSiteGroups = contentSequenceArr.filter(function (group) {
        return codeValueMatch$1(group, FINDING_SITE$2, FINDING_SITE_OLD$1);
      }) || [];
      var NUMGroup = contentSequenceArr.find(function (group) {
        return group.ValueType === "NUM";
      });
      var SCOORDGroup = toArray(NUMGroup.ContentSequence).find(function (group) {
        return group.ValueType === "SCOORD";
      });
      var ReferencedSOPSequence = SCOORDGroup.ContentSequence.ReferencedSOPSequence;
      var ReferencedSOPInstanceUID = ReferencedSOPSequence.ReferencedSOPInstanceUID,
        ReferencedFrameNumber = ReferencedSOPSequence.ReferencedFrameNumber;
      var defaultState = {
        sopInstanceUid: ReferencedSOPInstanceUID,
        frameIndex: ReferencedFrameNumber || 1,
        complete: true,
        finding: findingGroup ? addAccessors$1(findingGroup.ConceptCodeSequence) : undefined,
        findingSites: findingSiteGroups.map(function (fsg) {
          return addAccessors$1(fsg.ConceptCodeSequence);
        })
      };
      if (defaultState.finding) {
        defaultState.description = defaultState.finding.CodeMeaning;
      }
      var findingSite = defaultState.findingSites && defaultState.findingSites[0];
      if (findingSite) {
        defaultState.location = findingSite[0] && findingSite[0].CodeMeaning || findingSite.CodeMeaning;
      }
      return {
        defaultState: defaultState,
        findingGroup: findingGroup,
        findingSiteGroups: findingSiteGroups,
        NUMGroup: NUMGroup,
        SCOORDGroup: SCOORDGroup,
        ReferencedSOPSequence: ReferencedSOPSequence,
        ReferencedSOPInstanceUID: ReferencedSOPInstanceUID,
        ReferencedFrameNumber: ReferencedFrameNumber
      };
    }
  }, {
    key: "generateReport",
    value: function generateReport(toolState, metadataProvider, options) {
      // ToolState for array of imageIDs to a Report
      // Assume Cornerstone metadata provider has access to Study / Series / Sop Instance UID

      var allMeasurementGroups = [];
      var firstImageId = Object.keys(toolState)[0];
      if (!firstImageId) {
        throw new Error("No measurements provided.");
      }

      /* Patient ID
      Warning - Missing attribute or value that would be needed to build DICOMDIR - Patient ID
      Warning - Missing attribute or value that would be needed to build DICOMDIR - Study Date
      Warning - Missing attribute or value that would be needed to build DICOMDIR - Study Time
      Warning - Missing attribute or value that would be needed to build DICOMDIR - Study ID
       */
      var generalSeriesModule = metadataProvider.get("generalSeriesModule", firstImageId);

      //const sopCommonModule = metadataProvider.get('sopCommonModule', firstImageId);

      // NOTE: We are getting the Series and Study UIDs from the first imageId of the toolState
      // which means that if the toolState is for multiple series, the report will have the incorrect
      // SeriesInstanceUIDs
      var studyInstanceUID = generalSeriesModule.studyInstanceUID,
        seriesInstanceUID = generalSeriesModule.seriesInstanceUID;

      // Loop through each image in the toolData
      Object.keys(toolState).forEach(function (imageId) {
        var sopCommonModule = metadataProvider.get("sopCommonModule", imageId);
        var frameNumber = metadataProvider.get("frameNumber", imageId);
        var toolData = toolState[imageId];
        var toolTypes = Object.keys(toolData);
        var ReferencedSOPSequence = {
          ReferencedSOPClassUID: sopCommonModule.sopClassUID,
          ReferencedSOPInstanceUID: sopCommonModule.sopInstanceUID
        };
        if (Normalizer$4.isMultiframeSOPClassUID(sopCommonModule.sopClassUID)) {
          ReferencedSOPSequence.ReferencedFrameNumber = frameNumber;
        }

        // Loop through each tool type for the image
        var measurementGroups = [];
        toolTypes.forEach(function (toolType) {
          var group = getMeasurementGroup$1(toolType, toolData, ReferencedSOPSequence);
          if (group) {
            measurementGroups.push(group);
          }
        });
        allMeasurementGroups = allMeasurementGroups.concat(measurementGroups);
      });
      var _MeasurementReport = new TID1500MeasurementReport$1({
        TID1501MeasurementGroups: allMeasurementGroups
      }, options);

      // TODO: what is the correct metaheader
      // http://dicom.nema.org/medical/Dicom/current/output/chtml/part10/chapter_7.html
      // TODO: move meta creation to happen in derivations.js
      var fileMetaInformationVersionArray = new Uint8Array(2);
      fileMetaInformationVersionArray[1] = 1;
      var derivationSourceDataset = {
        StudyInstanceUID: studyInstanceUID,
        SeriesInstanceUID: seriesInstanceUID
        //SOPInstanceUID: sopInstanceUID, // TODO: Necessary?
        //SOPClassUID: sopClassUID,
      };

      var _meta = {
        FileMetaInformationVersion: {
          Value: [fileMetaInformationVersionArray.buffer],
          vr: "OB"
        },
        //MediaStorageSOPClassUID
        //MediaStorageSOPInstanceUID: sopCommonModule.sopInstanceUID,
        TransferSyntaxUID: {
          Value: ["1.2.840.10008.1.2.1"],
          vr: "UI"
        },
        ImplementationClassUID: {
          Value: [DicomMetaDictionary$4.uid()],
          // TODO: could be git hash or other valid id
          vr: "UI"
        },
        ImplementationVersionName: {
          Value: ["dcmjs"],
          vr: "SH"
        }
      };
      var _vrMap = {
        PixelData: "OW"
      };
      derivationSourceDataset._meta = _meta;
      derivationSourceDataset._vrMap = _vrMap;
      var report = new StructuredReport$1([derivationSourceDataset]);
      var contentItem = _MeasurementReport.contentItem(derivationSourceDataset);

      // Merge the derived dataset with the content from the Measurement Report
      report.dataset = Object.assign(report.dataset, contentItem);
      report.dataset._meta = _meta;
      return report;
    }

    /**
     * Generate Cornerstone tool state from dataset
     * @param {object} dataset dataset
     * @param {object} hooks
     * @param {function} hooks.getToolClass Function to map dataset to a tool class
     * @returns
     */
  }, {
    key: "generateToolState",
    value: function generateToolState(dataset) {
      var hooks = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
      // For now, bail out if the dataset is not a TID1500 SR with length measurements
      if (dataset.ContentTemplateSequence.TemplateIdentifier !== "1500") {
        throw new Error("This package can currently only interpret DICOM SR TID 1500");
      }
      var REPORT = "Imaging Measurements";
      var GROUP = "Measurement Group";
      var TRACKING_IDENTIFIER = "Tracking Identifier";

      // Identify the Imaging Measurements
      var imagingMeasurementContent = toArray(dataset.ContentSequence).find(codeMeaningEquals(REPORT));

      // Retrieve the Measurements themselves
      var measurementGroups = toArray(imagingMeasurementContent.ContentSequence).filter(codeMeaningEquals(GROUP));

      // For each of the supported measurement types, compute the measurement data
      var measurementData = {};
      var cornerstoneToolClasses = MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_UTILITY_TYPE;
      var registeredToolClasses = [];
      Object.keys(cornerstoneToolClasses).forEach(function (key) {
        registeredToolClasses.push(cornerstoneToolClasses[key]);
        measurementData[key] = [];
      });
      measurementGroups.forEach(function (measurementGroup) {
        var measurementGroupContentSequence = toArray(measurementGroup.ContentSequence);
        var TrackingIdentifierGroup = measurementGroupContentSequence.find(function (contentItem) {
          return contentItem.ConceptNameCodeSequence.CodeMeaning === TRACKING_IDENTIFIER;
        });
        var TrackingIdentifierValue = TrackingIdentifierGroup.TextValue;
        var toolClass = hooks.getToolClass ? hooks.getToolClass(measurementGroup, dataset, registeredToolClasses) : registeredToolClasses.find(function (tc) {
          return tc.isValidCornerstoneTrackingIdentifier(TrackingIdentifierValue);
        });
        if (toolClass) {
          var measurement = toolClass.getMeasurementData(measurementGroup);
          console.log("=== ".concat(toolClass.toolType, " ==="));
          console.log(measurement);
          measurementData[toolClass.toolType].push(measurement);
        }
      });

      // NOTE: There is no way of knowing the cornerstone imageIds as that could be anything.
      // That is up to the consumer to derive from the SOPInstanceUIDs.
      return measurementData;
    }
  }, {
    key: "registerTool",
    value: function registerTool(toolClass) {
      MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_UTILITY_TYPE[toolClass.utilityToolType] = toolClass;
      MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_TOOL_TYPE[toolClass.toolType] = toolClass;
      MeasurementReport.MEASUREMENT_BY_TOOLTYPE[toolClass.toolType] = toolClass.utilityToolType;
    }
  }]);
  return MeasurementReport;
}();
MeasurementReport$1.MEASUREMENT_BY_TOOLTYPE = {};
MeasurementReport$1.CORNERSTONE_TOOL_CLASSES_BY_UTILITY_TYPE = {};
MeasurementReport$1.CORNERSTONE_TOOL_CLASSES_BY_TOOL_TYPE = {};

var CORNERSTONE_4_TAG = "cornerstoneTools@^4.0.0";

var TID300Length$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Length;
var LENGTH$1 = "Length";
var Length$1 = /*#__PURE__*/function () {
  function Length() {
    _classCallCheck(this, Length);
  }
  _createClass(Length, null, [{
    key: "getMeasurementData",
    value:
    // TODO: this function is required for all Cornerstone Tool Adapters, since it is called by MeasurementReport.
    function getMeasurementData(MeasurementGroup) {
      var _MeasurementReport$ge = MeasurementReport$1.getSetupMeasurementData(MeasurementGroup),
        defaultState = _MeasurementReport$ge.defaultState,
        NUMGroup = _MeasurementReport$ge.NUMGroup,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup;
      var state = _objectSpread2(_objectSpread2({}, defaultState), {}, {
        length: NUMGroup.MeasuredValueSequence.NumericValue,
        toolType: Length.toolType,
        handles: {
          start: {},
          end: {},
          textBox: {
            hasMoved: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true
          }
        }
      });
      var _SCOORDGroup$GraphicD = _slicedToArray(SCOORDGroup.GraphicData, 4);
      state.handles.start.x = _SCOORDGroup$GraphicD[0];
      state.handles.start.y = _SCOORDGroup$GraphicD[1];
      state.handles.end.x = _SCOORDGroup$GraphicD[2];
      state.handles.end.y = _SCOORDGroup$GraphicD[3];
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var handles = tool.handles,
        finding = tool.finding,
        findingSites = tool.findingSites;
      var point1 = handles.start;
      var point2 = handles.end;
      var distance = tool.length;
      var trackingIdentifierTextValue = "cornerstoneTools@^4.0.0:Length";
      return {
        point1: point1,
        point2: point2,
        distance: distance,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return Length;
}();
Length$1.toolType = LENGTH$1;
Length$1.utilityToolType = LENGTH$1;
Length$1.TID300Representation = TID300Length$1;
Length$1.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === LENGTH$1;
};
MeasurementReport$1.registerTool(Length$1);

var TID300Polyline$3 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Polyline;
var FreehandRoi = /*#__PURE__*/function () {
  function FreehandRoi() {
    _classCallCheck(this, FreehandRoi);
  }
  _createClass(FreehandRoi, null, [{
    key: "getMeasurementData",
    value: function getMeasurementData(MeasurementGroup) {
      var _MeasurementReport$ge = MeasurementReport$1.getSetupMeasurementData(MeasurementGroup),
        defaultState = _MeasurementReport$ge.defaultState,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup,
        NUMGroup = _MeasurementReport$ge.NUMGroup;
      var state = _objectSpread2(_objectSpread2({}, defaultState), {}, {
        toolType: FreehandRoi.toolType,
        handles: {
          points: [],
          textBox: {
            active: false,
            hasMoved: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true
          }
        },
        cachedStats: {
          area: NUMGroup ? NUMGroup.MeasuredValueSequence.NumericValue : 0
        },
        color: undefined,
        invalidated: true
      });
      var GraphicData = SCOORDGroup.GraphicData;
      for (var i = 0; i < GraphicData.length; i += 2) {
        state.handles.points.push({
          x: GraphicData[i],
          y: GraphicData[i + 1]
        });
      }
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var handles = tool.handles,
        finding = tool.finding,
        findingSites = tool.findingSites,
        _tool$cachedStats = tool.cachedStats,
        cachedStats = _tool$cachedStats === void 0 ? {} : _tool$cachedStats;
      var points = handles.points;
      var _cachedStats$area = cachedStats.area,
        area = _cachedStats$area === void 0 ? 0 : _cachedStats$area,
        _cachedStats$perimete = cachedStats.perimeter,
        perimeter = _cachedStats$perimete === void 0 ? 0 : _cachedStats$perimete;
      var trackingIdentifierTextValue = "cornerstoneTools@^4.0.0:FreehandRoi";
      return {
        points: points,
        area: area,
        perimeter: perimeter,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return FreehandRoi;
}();
FreehandRoi.toolType = "FreehandRoi";
FreehandRoi.utilityToolType = "FreehandRoi";
FreehandRoi.TID300Representation = TID300Polyline$3;
FreehandRoi.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === FreehandRoi.toolType;
};
MeasurementReport$1.registerTool(FreehandRoi);

var TID300Bidirectional$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Bidirectional;
var BIDIRECTIONAL$1 = "Bidirectional";
var LONG_AXIS$1 = "Long Axis";
var SHORT_AXIS$1 = "Short Axis";
var FINDING$1 = "121071";
var FINDING_SITE$1 = "G-C0E3";
var Bidirectional$1 = /*#__PURE__*/function () {
  function Bidirectional() {
    _classCallCheck(this, Bidirectional);
  }
  _createClass(Bidirectional, null, [{
    key: "getMeasurementData",
    value:
    // TODO: this function is required for all Cornerstone Tool Adapters, since it is called by MeasurementReport.
    function getMeasurementData(MeasurementGroup) {
      var ContentSequence = MeasurementGroup.ContentSequence;
      var findingGroup = toArray(ContentSequence).find(function (group) {
        return group.ConceptNameCodeSequence.CodeValue === FINDING$1;
      });
      var findingSiteGroups = toArray(ContentSequence).filter(function (group) {
        return group.ConceptNameCodeSequence.CodeValue === FINDING_SITE$1;
      });
      var longAxisNUMGroup = toArray(ContentSequence).find(function (group) {
        return group.ConceptNameCodeSequence.CodeMeaning === LONG_AXIS$1;
      });
      var longAxisSCOORDGroup = toArray(longAxisNUMGroup.ContentSequence).find(function (group) {
        return group.ValueType === "SCOORD";
      });
      var shortAxisNUMGroup = toArray(ContentSequence).find(function (group) {
        return group.ConceptNameCodeSequence.CodeMeaning === SHORT_AXIS$1;
      });
      var shortAxisSCOORDGroup = toArray(shortAxisNUMGroup.ContentSequence).find(function (group) {
        return group.ValueType === "SCOORD";
      });
      var ReferencedSOPSequence = longAxisSCOORDGroup.ContentSequence.ReferencedSOPSequence;
      var ReferencedSOPInstanceUID = ReferencedSOPSequence.ReferencedSOPInstanceUID,
        ReferencedFrameNumber = ReferencedSOPSequence.ReferencedFrameNumber;

      // Long axis

      var longestDiameter = String(longAxisNUMGroup.MeasuredValueSequence.NumericValue);
      var shortestDiameter = String(shortAxisNUMGroup.MeasuredValueSequence.NumericValue);
      var bottomRight = {
        x: Math.max(longAxisSCOORDGroup.GraphicData[0], longAxisSCOORDGroup.GraphicData[2], shortAxisSCOORDGroup.GraphicData[0], shortAxisSCOORDGroup.GraphicData[2]),
        y: Math.max(longAxisSCOORDGroup.GraphicData[1], longAxisSCOORDGroup.GraphicData[3], shortAxisSCOORDGroup.GraphicData[1], shortAxisSCOORDGroup.GraphicData[3])
      };
      var state = {
        sopInstanceUid: ReferencedSOPInstanceUID,
        frameIndex: ReferencedFrameNumber || 1,
        toolType: Bidirectional.toolType,
        active: false,
        handles: {
          start: {
            x: longAxisSCOORDGroup.GraphicData[0],
            y: longAxisSCOORDGroup.GraphicData[1],
            drawnIndependently: false,
            allowedOutsideImage: false,
            active: false,
            highlight: false,
            index: 0
          },
          end: {
            x: longAxisSCOORDGroup.GraphicData[2],
            y: longAxisSCOORDGroup.GraphicData[3],
            drawnIndependently: false,
            allowedOutsideImage: false,
            active: false,
            highlight: false,
            index: 1
          },
          perpendicularStart: {
            x: shortAxisSCOORDGroup.GraphicData[0],
            y: shortAxisSCOORDGroup.GraphicData[1],
            drawnIndependently: false,
            allowedOutsideImage: false,
            active: false,
            highlight: false,
            index: 2
          },
          perpendicularEnd: {
            x: shortAxisSCOORDGroup.GraphicData[2],
            y: shortAxisSCOORDGroup.GraphicData[3],
            drawnIndependently: false,
            allowedOutsideImage: false,
            active: false,
            highlight: false,
            index: 3
          },
          textBox: {
            highlight: false,
            hasMoved: true,
            active: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true,
            x: bottomRight.x + 10,
            y: bottomRight.y + 10
          }
        },
        invalidated: false,
        isCreating: false,
        longestDiameter: longestDiameter,
        shortestDiameter: shortestDiameter,
        toolName: "Bidirectional",
        visible: true,
        finding: findingGroup ? findingGroup.ConceptCodeSequence : undefined,
        findingSites: findingSiteGroups.map(function (fsg) {
          return fsg.ConceptCodeSequence;
        })
      };
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var _tool$handles = tool.handles,
        start = _tool$handles.start,
        end = _tool$handles.end,
        perpendicularStart = _tool$handles.perpendicularStart,
        perpendicularEnd = _tool$handles.perpendicularEnd;
      var shortestDiameter = tool.shortestDiameter,
        longestDiameter = tool.longestDiameter,
        finding = tool.finding,
        findingSites = tool.findingSites;
      var trackingIdentifierTextValue = "cornerstoneTools@^4.0.0:Bidirectional";
      return {
        longAxis: {
          point1: start,
          point2: end
        },
        shortAxis: {
          point1: perpendicularStart,
          point2: perpendicularEnd
        },
        longAxisLength: longestDiameter,
        shortAxisLength: shortestDiameter,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return Bidirectional;
}();
Bidirectional$1.toolType = BIDIRECTIONAL$1;
Bidirectional$1.utilityToolType = BIDIRECTIONAL$1;
Bidirectional$1.TID300Representation = TID300Bidirectional$1;
Bidirectional$1.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === BIDIRECTIONAL$1;
};
MeasurementReport$1.registerTool(Bidirectional$1);

var TID300Ellipse$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Ellipse;
var ELLIPTICALROI$1 = "EllipticalRoi";
var EllipticalRoi = /*#__PURE__*/function () {
  function EllipticalRoi() {
    _classCallCheck(this, EllipticalRoi);
  }
  _createClass(EllipticalRoi, null, [{
    key: "getMeasurementData",
    value:
    // TODO: this function is required for all Cornerstone Tool Adapters, since it is called by MeasurementReport.
    function getMeasurementData(MeasurementGroup) {
      var _MeasurementReport$ge = MeasurementReport$1.getSetupMeasurementData(MeasurementGroup),
        defaultState = _MeasurementReport$ge.defaultState,
        NUMGroup = _MeasurementReport$ge.NUMGroup,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup;
      var GraphicData = SCOORDGroup.GraphicData;
      var majorAxis = [{
        x: GraphicData[0],
        y: GraphicData[1]
      }, {
        x: GraphicData[2],
        y: GraphicData[3]
      }];
      var minorAxis = [{
        x: GraphicData[4],
        y: GraphicData[5]
      }, {
        x: GraphicData[6],
        y: GraphicData[7]
      }];

      // Calculate two opposite corners of box defined by two axes.

      var minorAxisLength = Math.sqrt(Math.pow(minorAxis[0].x - minorAxis[1].x, 2) + Math.pow(minorAxis[0].y - minorAxis[1].y, 2));
      var minorAxisDirection = {
        x: (minorAxis[1].x - minorAxis[0].x) / minorAxisLength,
        y: (minorAxis[1].y - minorAxis[0].y) / minorAxisLength
      };
      var halfMinorAxisLength = minorAxisLength / 2;

      // First end point of major axis + half minor axis vector
      var corner1 = {
        x: majorAxis[0].x + minorAxisDirection.x * halfMinorAxisLength,
        y: majorAxis[0].y + minorAxisDirection.y * halfMinorAxisLength
      };

      // Second end point of major axis - half of minor axis vector
      var corner2 = {
        x: majorAxis[1].x - minorAxisDirection.x * halfMinorAxisLength,
        y: majorAxis[1].y - minorAxisDirection.y * halfMinorAxisLength
      };
      var state = _objectSpread2(_objectSpread2({}, defaultState), {}, {
        toolType: EllipticalRoi.toolType,
        active: false,
        cachedStats: {
          area: NUMGroup ? NUMGroup.MeasuredValueSequence.NumericValue : 0
        },
        handles: {
          end: {
            x: corner1.x,
            y: corner1.y,
            highlight: false,
            active: false
          },
          initialRotation: 0,
          start: {
            x: corner2.x,
            y: corner2.y,
            highlight: false,
            active: false
          },
          textBox: {
            hasMoved: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true
          }
        },
        invalidated: true,
        visible: true
      });
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var _tool$cachedStats = tool.cachedStats,
        cachedStats = _tool$cachedStats === void 0 ? {} : _tool$cachedStats,
        handles = tool.handles,
        finding = tool.finding,
        findingSites = tool.findingSites;
      var start = handles.start,
        end = handles.end;
      var area = cachedStats.area;
      var halfXLength = Math.abs(start.x - end.x) / 2;
      var halfYLength = Math.abs(start.y - end.y) / 2;
      var points = [];
      var center = {
        x: (start.x + end.x) / 2,
        y: (start.y + end.y) / 2
      };
      if (halfXLength > halfYLength) {
        // X-axis major
        // Major axis
        points.push({
          x: center.x - halfXLength,
          y: center.y
        });
        points.push({
          x: center.x + halfXLength,
          y: center.y
        });
        // Minor axis
        points.push({
          x: center.x,
          y: center.y - halfYLength
        });
        points.push({
          x: center.x,
          y: center.y + halfYLength
        });
      } else {
        // Y-axis major
        // Major axis
        points.push({
          x: center.x,
          y: center.y - halfYLength
        });
        points.push({
          x: center.x,
          y: center.y + halfYLength
        });
        // Minor axis
        points.push({
          x: center.x - halfXLength,
          y: center.y
        });
        points.push({
          x: center.x + halfXLength,
          y: center.y
        });
      }
      var trackingIdentifierTextValue = "cornerstoneTools@^4.0.0:EllipticalRoi";
      return {
        area: area,
        points: points,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return EllipticalRoi;
}();
EllipticalRoi.toolType = ELLIPTICALROI$1;
EllipticalRoi.utilityToolType = ELLIPTICALROI$1;
EllipticalRoi.TID300Representation = TID300Ellipse$1;
EllipticalRoi.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === ELLIPTICALROI$1;
};
MeasurementReport$1.registerTool(EllipticalRoi);

var TID300Circle$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Circle;
var CIRCLEROI$1 = "CircleRoi";
var CircleRoi = /*#__PURE__*/function () {
  function CircleRoi() {
    _classCallCheck(this, CircleRoi);
  }
  _createClass(CircleRoi, null, [{
    key: "getMeasurementData",
    value: /** Gets the measurement data for cornerstone, given DICOM SR measurement data. */
    function getMeasurementData(MeasurementGroup) {
      var _MeasurementReport$ge = MeasurementReport$1.getSetupMeasurementData(MeasurementGroup),
        defaultState = _MeasurementReport$ge.defaultState,
        NUMGroup = _MeasurementReport$ge.NUMGroup,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup;
      var GraphicData = SCOORDGroup.GraphicData;
      var center = {
        x: GraphicData[0],
        y: GraphicData[1]
      };
      var end = {
        x: GraphicData[2],
        y: GraphicData[3]
      };
      var state = _objectSpread2(_objectSpread2({}, defaultState), {}, {
        toolType: CircleRoi.toolType,
        active: false,
        cachedStats: {
          area: NUMGroup ? NUMGroup.MeasuredValueSequence.NumericValue : 0,
          // Dummy values to be updated by cornerstone
          radius: 0,
          perimeter: 0
        },
        handles: {
          end: _objectSpread2(_objectSpread2({}, end), {}, {
            highlight: false,
            active: false
          }),
          initialRotation: 0,
          start: _objectSpread2(_objectSpread2({}, center), {}, {
            highlight: false,
            active: false
          }),
          textBox: {
            hasMoved: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true
          }
        },
        invalidated: true,
        visible: true
      });
      return state;
    }

    /**
     * Gets the TID 300 representation of a circle, given the cornerstone representation.
     *
     * @param {Object} tool
     * @returns
     */
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var _tool$cachedStats = tool.cachedStats,
        cachedStats = _tool$cachedStats === void 0 ? {} : _tool$cachedStats,
        handles = tool.handles,
        finding = tool.finding,
        findingSites = tool.findingSites;
      var center = handles.start,
        end = handles.end;
      var area = cachedStats.area,
        radius = cachedStats.radius;
      var perimeter = 2 * Math.PI * radius;
      var points = [];
      points.push(center);
      points.push(end);
      var trackingIdentifierTextValue = "cornerstoneTools@^4.0.0:CircleRoi";
      return {
        area: area,
        perimeter: perimeter,
        radius: radius,
        points: points,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return CircleRoi;
}();
CircleRoi.toolType = CIRCLEROI$1;
CircleRoi.utilityToolType = CIRCLEROI$1;
CircleRoi.TID300Representation = TID300Circle$1;
CircleRoi.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === CIRCLEROI$1;
};
MeasurementReport$1.registerTool(CircleRoi);

var TID300Point$2 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Point;
var ARROW_ANNOTATE$1 = "ArrowAnnotate";
var CORNERSTONEFREETEXT$1 = "CORNERSTONEFREETEXT";
var ArrowAnnotate$1 = /*#__PURE__*/function () {
  function ArrowAnnotate() {
    _classCallCheck(this, ArrowAnnotate);
  }
  _createClass(ArrowAnnotate, null, [{
    key: "getMeasurementData",
    value: function getMeasurementData(MeasurementGroup) {
      var _MeasurementReport$ge = MeasurementReport$1.getSetupMeasurementData(MeasurementGroup),
        defaultState = _MeasurementReport$ge.defaultState,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup,
        findingGroup = _MeasurementReport$ge.findingGroup;
      var text = findingGroup.ConceptCodeSequence.CodeMeaning;
      var GraphicData = SCOORDGroup.GraphicData;
      var state = _objectSpread2(_objectSpread2({}, defaultState), {}, {
        toolType: ArrowAnnotate.toolType,
        active: false,
        handles: {
          start: {
            x: GraphicData[0],
            y: GraphicData[1],
            highlight: true,
            active: false
          },
          // Use a generic offset if the stored data doesn't have the endpoint, otherwise
          // use the actual endpoint.
          end: {
            x: GraphicData.length == 4 ? GraphicData[2] : GraphicData[0] + 20,
            y: GraphicData.length == 4 ? GraphicData[3] : GraphicData[1] + 20,
            highlight: true,
            active: false
          },
          textBox: {
            hasMoved: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true
          }
        },
        invalidated: true,
        text: text,
        visible: true
      });
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var points = [tool.handles.start, tool.handles.end];
      var finding = tool.finding,
        findingSites = tool.findingSites;
      var TID300RepresentationArguments = {
        points: points,
        trackingIdentifierTextValue: "cornerstoneTools@^4.0.0:ArrowAnnotate",
        findingSites: findingSites || []
      };

      // If freetext finding isn't present, add it from the tool text.
      if (!finding || finding.CodeValue !== CORNERSTONEFREETEXT$1) {
        finding = {
          CodeValue: CORNERSTONEFREETEXT$1,
          CodingSchemeDesignator: "CST4",
          CodeMeaning: tool.text
        };
      }
      TID300RepresentationArguments.finding = finding;
      return TID300RepresentationArguments;
    }
  }]);
  return ArrowAnnotate;
}();
ArrowAnnotate$1.toolType = ARROW_ANNOTATE$1;
ArrowAnnotate$1.utilityToolType = ARROW_ANNOTATE$1;
ArrowAnnotate$1.TID300Representation = TID300Point$2;
ArrowAnnotate$1.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === ARROW_ANNOTATE$1;
};
MeasurementReport$1.registerTool(ArrowAnnotate$1);

var TID300CobbAngle$2 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.CobbAngle;
var COBB_ANGLE = "CobbAngle";
var CobbAngle$1 = /*#__PURE__*/function () {
  function CobbAngle() {
    _classCallCheck(this, CobbAngle);
  }
  _createClass(CobbAngle, null, [{
    key: "getMeasurementData",
    value:
    // TODO: this function is required for all Cornerstone Tool Adapters, since it is called by MeasurementReport.
    function getMeasurementData(MeasurementGroup) {
      var _MeasurementReport$ge = MeasurementReport$1.getSetupMeasurementData(MeasurementGroup),
        defaultState = _MeasurementReport$ge.defaultState,
        NUMGroup = _MeasurementReport$ge.NUMGroup,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup;
      var state = _objectSpread2(_objectSpread2({}, defaultState), {}, {
        rAngle: NUMGroup.MeasuredValueSequence.NumericValue,
        toolType: CobbAngle.toolType,
        handles: {
          start: {},
          end: {},
          start2: {
            highlight: true,
            drawnIndependently: true
          },
          end2: {
            highlight: true,
            drawnIndependently: true
          },
          textBox: {
            hasMoved: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true
          }
        }
      });
      var _SCOORDGroup$GraphicD = _slicedToArray(SCOORDGroup.GraphicData, 8);
      state.handles.start.x = _SCOORDGroup$GraphicD[0];
      state.handles.start.y = _SCOORDGroup$GraphicD[1];
      state.handles.end.x = _SCOORDGroup$GraphicD[2];
      state.handles.end.y = _SCOORDGroup$GraphicD[3];
      state.handles.start2.x = _SCOORDGroup$GraphicD[4];
      state.handles.start2.y = _SCOORDGroup$GraphicD[5];
      state.handles.end2.x = _SCOORDGroup$GraphicD[6];
      state.handles.end2.y = _SCOORDGroup$GraphicD[7];
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var handles = tool.handles,
        finding = tool.finding,
        findingSites = tool.findingSites;
      var point1 = handles.start;
      var point2 = handles.end;
      var point3 = handles.start2;
      var point4 = handles.end2;
      var rAngle = tool.rAngle;
      var trackingIdentifierTextValue = "cornerstoneTools@^4.0.0:CobbAngle";
      return {
        point1: point1,
        point2: point2,
        point3: point3,
        point4: point4,
        rAngle: rAngle,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return CobbAngle;
}();
CobbAngle$1.toolType = COBB_ANGLE;
CobbAngle$1.utilityToolType = COBB_ANGLE;
CobbAngle$1.TID300Representation = TID300CobbAngle$2;
CobbAngle$1.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === COBB_ANGLE;
};
MeasurementReport$1.registerTool(CobbAngle$1);

var TID300Angle = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Angle;
var ANGLE = "Angle";
var Angle$1 = /*#__PURE__*/function () {
  function Angle() {
    _classCallCheck(this, Angle);
  }
  _createClass(Angle, null, [{
    key: "getMeasurementData",
    value:
    /**
     * Generate TID300 measurement data for a plane angle measurement - use a Angle, but label it as Angle
     */
    function getMeasurementData(MeasurementGroup) {
      var _MeasurementReport$ge = MeasurementReport$1.getSetupMeasurementData(MeasurementGroup),
        defaultState = _MeasurementReport$ge.defaultState,
        NUMGroup = _MeasurementReport$ge.NUMGroup,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup;
      var state = _objectSpread2(_objectSpread2({}, defaultState), {}, {
        rAngle: NUMGroup.MeasuredValueSequence.NumericValue,
        toolType: Angle.toolType,
        handles: {
          start: {},
          middle: {},
          end: {},
          textBox: {
            hasMoved: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true
          }
        }
      });
      var _SCOORDGroup$GraphicD = _slicedToArray(SCOORDGroup.GraphicData, 8);
      state.handles.start.x = _SCOORDGroup$GraphicD[0];
      state.handles.start.y = _SCOORDGroup$GraphicD[1];
      state.handles.middle.x = _SCOORDGroup$GraphicD[2];
      state.handles.middle.y = _SCOORDGroup$GraphicD[3];
      state.handles.middle.x = _SCOORDGroup$GraphicD[4];
      state.handles.middle.y = _SCOORDGroup$GraphicD[5];
      state.handles.end.x = _SCOORDGroup$GraphicD[6];
      state.handles.end.y = _SCOORDGroup$GraphicD[7];
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var handles = tool.handles,
        finding = tool.finding,
        findingSites = tool.findingSites;
      var point1 = handles.start;
      var point2 = handles.middle;
      var point3 = handles.middle;
      var point4 = handles.end;
      var rAngle = tool.rAngle;
      var trackingIdentifierTextValue = "cornerstoneTools@^4.0.0:Angle";
      return {
        point1: point1,
        point2: point2,
        point3: point3,
        point4: point4,
        rAngle: rAngle,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return Angle;
}();
Angle$1.toolType = ANGLE;
Angle$1.utilityToolType = ANGLE;
Angle$1.TID300Representation = TID300Angle;
Angle$1.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === ANGLE;
};
MeasurementReport$1.registerTool(Angle$1);

var TID300Polyline$2 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Polyline;
var RectangleRoi = /*#__PURE__*/function () {
  function RectangleRoi() {
    _classCallCheck(this, RectangleRoi);
  }
  _createClass(RectangleRoi, null, [{
    key: "getMeasurementData",
    value: function getMeasurementData(MeasurementGroup) {
      var _MeasurementReport$ge = MeasurementReport$1.getSetupMeasurementData(MeasurementGroup),
        defaultState = _MeasurementReport$ge.defaultState,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup,
        NUMGroup = _MeasurementReport$ge.NUMGroup;
      var state = _objectSpread2(_objectSpread2({}, defaultState), {}, {
        toolType: RectangleRoi.toolType,
        handles: {
          start: {},
          end: {},
          textBox: {
            active: false,
            hasMoved: false,
            movesIndependently: false,
            drawnIndependently: true,
            allowedOutsideImage: true,
            hasBoundingBox: true
          },
          initialRotation: 0
        },
        cachedStats: {
          area: NUMGroup ? NUMGroup.MeasuredValueSequence.NumericValue : 0
        },
        color: undefined,
        invalidated: true
      });
      var _SCOORDGroup$GraphicD = _slicedToArray(SCOORDGroup.GraphicData, 6);
      state.handles.start.x = _SCOORDGroup$GraphicD[0];
      state.handles.start.y = _SCOORDGroup$GraphicD[1];
      _SCOORDGroup$GraphicD[2];
      _SCOORDGroup$GraphicD[3];
      state.handles.end.x = _SCOORDGroup$GraphicD[4];
      state.handles.end.y = _SCOORDGroup$GraphicD[5];
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool) {
      var finding = tool.finding,
        findingSites = tool.findingSites,
        _tool$cachedStats = tool.cachedStats,
        cachedStats = _tool$cachedStats === void 0 ? {} : _tool$cachedStats,
        handles = tool.handles;
      var start = handles.start,
        end = handles.end;
      var points = [start, {
        x: start.x,
        y: end.y
      }, end, {
        x: end.x,
        y: start.y
      }];
      var area = cachedStats.area,
        perimeter = cachedStats.perimeter;
      var trackingIdentifierTextValue = "cornerstoneTools@^4.0.0:RectangleRoi";
      return {
        points: points,
        area: area,
        perimeter: perimeter,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return RectangleRoi;
}();
RectangleRoi.toolType = "RectangleRoi";
RectangleRoi.utilityToolType = "RectangleRoi";
RectangleRoi.TID300Representation = TID300Polyline$2;
RectangleRoi.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone4Tag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone4Tag !== CORNERSTONE_4_TAG) {
    return false;
  }
  return toolType === RectangleRoi.toolType;
};
MeasurementReport$1.registerTool(RectangleRoi);

var _utilities$orientatio$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.orientation,
  rotateDirectionCosinesInPlane$1 = _utilities$orientatio$1.rotateDirectionCosinesInPlane,
  flipIOP$1 = _utilities$orientatio$1.flipImageOrientationPatient,
  flipMatrix2D$1 = _utilities$orientatio$1.flipMatrix2D,
  rotateMatrix902D$1 = _utilities$orientatio$1.rotateMatrix902D;
var datasetToBlob = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.datasetToBlob,
  BitArray$2 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.BitArray,
  DicomMessage$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.DicomMessage,
  DicomMetaDictionary$3 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.DicomMetaDictionary;
var Normalizer$3 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .normalizers */ .oq.Normalizer;
var SegmentationDerivation$2 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .derivations */ .U7.Segmentation;
var Segmentation$5 = {
  generateSegmentation: generateSegmentation$3,
  generateToolState: generateToolState$3
};

/**
 *
 * @typedef {Object} BrushData
 * @property {Object} toolState - The cornerstoneTools global toolState.
 * @property {Object[]} segments - The cornerstoneTools segment metadata that corresponds to the
 *                                 seriesInstanceUid.
 */

/**
 * generateSegmentation - Generates cornerstoneTools brush data, given a stack of
 * imageIds, images and the cornerstoneTools brushData.
 *
 * @param  {object[]} images    An array of the cornerstone image objects.
 * @param  {BrushData} brushData and object containing the brushData.
 * @returns {type}           description
 */
function generateSegmentation$3(images, brushData) {
  var options = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {
    includeSliceSpacing: true
  };
  var toolState = brushData.toolState,
    segments = brushData.segments;

  // Calculate the dimensions of the data cube.
  var image0 = images[0];
  var dims = {
    x: image0.columns,
    y: image0.rows,
    z: images.length
  };
  dims.xy = dims.x * dims.y;
  var numSegments = _getSegCount(seg, segments);
  if (!numSegments) {
    throw new Error("No segments to export!");
  }
  var isMultiframe = image0.imageId.includes("?frame");
  var seg = _createSegFromImages$1(images, isMultiframe, options);
  var _getNumberOfFramesPer = _getNumberOfFramesPerSegment(toolState, images, segments),
    referencedFramesPerSegment = _getNumberOfFramesPer.referencedFramesPerSegment,
    segmentIndicies = _getNumberOfFramesPer.segmentIndicies;
  var NumberOfFrames = 0;
  for (var i = 0; i < referencedFramesPerSegment.length; i++) {
    NumberOfFrames += referencedFramesPerSegment[i].length;
  }
  seg.setNumberOfFrames(NumberOfFrames);
  for (var _i = 0; _i < segmentIndicies.length; _i++) {
    var segmentIndex = segmentIndicies[_i];
    var referencedFrameIndicies = referencedFramesPerSegment[_i];

    // Frame numbers start from 1.
    var referencedFrameNumbers = referencedFrameIndicies.map(function (element) {
      return element + 1;
    });
    var segment = segments[segmentIndex];
    seg.addSegment(segment, _extractCornerstoneToolsPixelData(segmentIndex, referencedFrameIndicies, toolState, images, dims), referencedFrameNumbers);
  }
  seg.bitPackPixelData();
  var segBlob = datasetToBlob(seg.dataset);
  return segBlob;
}
function _extractCornerstoneToolsPixelData(segmentIndex, referencedFrames, toolState, images, dims) {
  var pixelData = new Uint8Array(dims.xy * referencedFrames.length);
  var pixelDataIndex = 0;
  for (var i = 0; i < referencedFrames.length; i++) {
    var frame = referencedFrames[i];
    var imageId = images[frame].imageId;
    var imageIdSpecificToolState = toolState[imageId];
    var brushPixelData = imageIdSpecificToolState.brush.data[segmentIndex].pixelData;
    for (var p = 0; p < brushPixelData.length; p++) {
      pixelData[pixelDataIndex] = brushPixelData[p];
      pixelDataIndex++;
    }
  }
  return pixelData;
}
function _getNumberOfFramesPerSegment(toolState, images, segments) {
  var segmentIndicies = [];
  var referencedFramesPerSegment = [];
  for (var i = 0; i < segments.length; i++) {
    if (segments[i]) {
      segmentIndicies.push(i);
      referencedFramesPerSegment.push([]);
    }
  }
  for (var z = 0; z < images.length; z++) {
    var imageId = images[z].imageId;
    var imageIdSpecificToolState = toolState[imageId];
    for (var _i2 = 0; _i2 < segmentIndicies.length; _i2++) {
      var segIdx = segmentIndicies[_i2];
      if (imageIdSpecificToolState && imageIdSpecificToolState.brush && imageIdSpecificToolState.brush.data && imageIdSpecificToolState.brush.data[segIdx] && imageIdSpecificToolState.brush.data[segIdx].pixelData) {
        referencedFramesPerSegment[_i2].push(z);
      }
    }
  }
  return {
    referencedFramesPerSegment: referencedFramesPerSegment,
    segmentIndicies: segmentIndicies
  };
}
function _getSegCount(seg, segments) {
  var numSegments = 0;
  for (var i = 0; i < segments.length; i++) {
    if (segments[i]) {
      numSegments++;
    }
  }
  return numSegments;
}

/**
 * _createSegFromImages - description
 *
 * @param  {Object[]} images    An array of the cornerstone image objects.
 * @param  {Boolean} isMultiframe Whether the images are multiframe.
 * @returns {Object}              The Seg derived dataSet.
 */
function _createSegFromImages$1(images, isMultiframe, options) {
  var datasets = [];
  if (isMultiframe) {
    var image = images[0];
    var arrayBuffer = image.data.byteArray.buffer;
    var dicomData = DicomMessage$1.readFile(arrayBuffer);
    var dataset = DicomMetaDictionary$3.naturalizeDataset(dicomData.dict);
    dataset._meta = DicomMetaDictionary$3.namifyDataset(dicomData.meta);
    datasets.push(dataset);
  } else {
    for (var i = 0; i < images.length; i++) {
      var _image = images[i];
      var _arrayBuffer = _image.data.byteArray.buffer;
      var _dicomData = DicomMessage$1.readFile(_arrayBuffer);
      var _dataset = DicomMetaDictionary$3.naturalizeDataset(_dicomData.dict);
      _dataset._meta = DicomMetaDictionary$3.namifyDataset(_dicomData.meta);
      datasets.push(_dataset);
    }
  }
  var multiframe = Normalizer$3.normalizeToDataset(datasets);
  return new SegmentationDerivation$2([multiframe], options);
}

/**
 * generateToolState - Given a set of cornrstoneTools imageIds and a Segmentation buffer,
 * derive cornerstoneTools toolState and brush metadata.
 *
 * @param  {string[]} imageIds    An array of the imageIds.
 * @param  {ArrayBuffer} arrayBuffer The SEG arrayBuffer.
 * @param {*} metadataProvider
 * @returns {Object}  The toolState and an object from which the
 *                    segment metadata can be derived.
 */
function generateToolState$3(imageIds, arrayBuffer, metadataProvider) {
  var dicomData = DicomMessage$1.readFile(arrayBuffer);
  var dataset = DicomMetaDictionary$3.naturalizeDataset(dicomData.dict);
  dataset._meta = DicomMetaDictionary$3.namifyDataset(dicomData.meta);
  var multiframe = Normalizer$3.normalizeToDataset([dataset]);
  var imagePlaneModule = metadataProvider.get("imagePlaneModule", imageIds[0]);
  if (!imagePlaneModule) {
    console.warn("Insufficient metadata, imagePlaneModule missing.");
  }
  var ImageOrientationPatient = Array.isArray(imagePlaneModule.rowCosines) ? [].concat(_toConsumableArray(imagePlaneModule.rowCosines), _toConsumableArray(imagePlaneModule.columnCosines)) : [imagePlaneModule.rowCosines.x, imagePlaneModule.rowCosines.y, imagePlaneModule.rowCosines.z, imagePlaneModule.columnCosines.x, imagePlaneModule.columnCosines.y, imagePlaneModule.columnCosines.z];

  // Get IOP from ref series, compute supported orientations:
  var validOrientations = getValidOrientations$1(ImageOrientationPatient);
  var SharedFunctionalGroupsSequence = multiframe.SharedFunctionalGroupsSequence;
  var sharedImageOrientationPatient = SharedFunctionalGroupsSequence.PlaneOrientationSequence ? SharedFunctionalGroupsSequence.PlaneOrientationSequence.ImageOrientationPatient : undefined;
  var sliceLength = multiframe.Columns * multiframe.Rows;
  var segMetadata = getSegmentMetadata$1(multiframe);
  var pixelData = unpackPixelData$1(multiframe);
  var PerFrameFunctionalGroupsSequence = multiframe.PerFrameFunctionalGroupsSequence;
  var toolState = {};
  var inPlane = true;
  for (var i = 0; i < PerFrameFunctionalGroupsSequence.length; i++) {
    var PerFrameFunctionalGroups = PerFrameFunctionalGroupsSequence[i];
    var ImageOrientationPatientI = sharedImageOrientationPatient || PerFrameFunctionalGroups.PlaneOrientationSequence.ImageOrientationPatient;
    var pixelDataI2D = ndarray__WEBPACK_IMPORTED_MODULE_2___default()(new Uint8Array(pixelData.buffer, i * sliceLength, sliceLength), [multiframe.Rows, multiframe.Columns]);
    var alignedPixelDataI = alignPixelDataWithSourceData$1(pixelDataI2D, ImageOrientationPatientI, validOrientations);
    if (!alignedPixelDataI) {
      console.warn("This segmentation object is not in-plane with the source data. Bailing out of IO. It'd be better to render this with vtkjs. ");
      inPlane = false;
      break;
    }
    var segmentIndex = PerFrameFunctionalGroups.SegmentIdentificationSequence.ReferencedSegmentNumber - 1;
    var SourceImageSequence = void 0;
    if (SharedFunctionalGroupsSequence.DerivationImageSequence && SharedFunctionalGroupsSequence.DerivationImageSequence.SourceImageSequence) {
      SourceImageSequence = SharedFunctionalGroupsSequence.DerivationImageSequence.SourceImageSequence[i];
    } else {
      SourceImageSequence = PerFrameFunctionalGroups.DerivationImageSequence.SourceImageSequence;
    }
    var imageId = getImageIdOfSourceImage(SourceImageSequence, imageIds, metadataProvider);
    addImageIdSpecificBrushToolState(toolState, imageId, segmentIndex, alignedPixelDataI);
  }
  if (!inPlane) {
    return;
  }
  return {
    toolState: toolState,
    segMetadata: segMetadata
  };
}

/**
 * unpackPixelData - Unpacks bitpacked pixelData if the Segmentation is BINARY.
 *
 * @param  {Object} multiframe The multiframe dataset.
 * @return {Uint8Array}      The unpacked pixelData.
 */
function unpackPixelData$1(multiframe) {
  var segType = multiframe.SegmentationType;
  if (segType === "BINARY") {
    return BitArray$2.unpack(multiframe.PixelData);
  }
  var pixelData = new Uint8Array(multiframe.PixelData);
  var max = multiframe.MaximumFractionalValue;
  var onlyMaxAndZero = pixelData.find(function (element) {
    return element !== 0 && element !== max;
  }) === undefined;
  if (!onlyMaxAndZero) {
    dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .log */ .cM.warn("This is a fractional segmentation, which is not currently supported.");
    return;
  }
  dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .log */ .cM.warn("This segmentation object is actually binary... processing as such.");
  return pixelData;
}

/**
 * addImageIdSpecificBrushToolState - Adds brush pixel data to cornerstoneTools
 * formatted toolState object.
 *
 * @param  {Object} toolState    The toolState object to modify
 * @param  {String} imageId      The imageId of the toolState to add the data.
 * @param  {Number} segmentIndex The index of the segment data being added.
 * @param  {Ndarray} pixelData2D  The pixelData in Ndarry 2D format.
 */
function addImageIdSpecificBrushToolState(toolState, imageId, segmentIndex, pixelData2D) {
  if (!toolState[imageId]) {
    toolState[imageId] = {};
    toolState[imageId].brush = {};
    toolState[imageId].brush.data = [];
  } else if (!toolState[imageId].brush) {
    toolState[imageId].brush = {};
    toolState[imageId].brush.data = [];
  } else if (!toolState[imageId].brush.data) {
    toolState[imageId].brush.data = [];
  }
  toolState[imageId].brush.data[segmentIndex] = {};
  var brushDataI = toolState[imageId].brush.data[segmentIndex];
  brushDataI.pixelData = new Uint8Array(pixelData2D.data.length);
  var cToolsPixelData = brushDataI.pixelData;
  for (var p = 0; p < cToolsPixelData.length; p++) {
    if (pixelData2D.data[p]) {
      cToolsPixelData[p] = 1;
    } else {
      cToolsPixelData[p] = 0;
    }
  }
}

/**
 * getImageIdOfSourceImage - Returns the Cornerstone imageId of the source image.
 *
 * @param  {Object} SourceImageSequence Sequence describing the source image.
 * @param  {String[]} imageIds          A list of imageIds.
 * @param  {Object} metadataProvider    A Cornerstone metadataProvider to query
 *                                      metadata from imageIds.
 * @return {String}                     The corresponding imageId.
 */
function getImageIdOfSourceImage(SourceImageSequence, imageIds, metadataProvider) {
  var ReferencedSOPInstanceUID = SourceImageSequence.ReferencedSOPInstanceUID,
    ReferencedFrameNumber = SourceImageSequence.ReferencedFrameNumber;
  return ReferencedFrameNumber ? getImageIdOfReferencedFrame$1(ReferencedSOPInstanceUID, ReferencedFrameNumber, imageIds, metadataProvider) : getImageIdOfReferencedSingleFramedSOPInstance(ReferencedSOPInstanceUID, imageIds, metadataProvider);
}

/**
 * getImageIdOfReferencedSingleFramedSOPInstance - Returns the imageId
 * corresponding to the specified sopInstanceUid for single-frame images.
 *
 * @param  {String} sopInstanceUid   The sopInstanceUid of the desired image.
 * @param  {String[]} imageIds         The list of imageIds.
 * @param  {Object} metadataProvider The metadataProvider to obtain sopInstanceUids
 *                                 from the cornerstone imageIds.
 * @return {String}                  The imageId that corresponds to the sopInstanceUid.
 */
function getImageIdOfReferencedSingleFramedSOPInstance(sopInstanceUid, imageIds, metadataProvider) {
  return imageIds.find(function (imageId) {
    var sopCommonModule = metadataProvider.get("sopCommonModule", imageId);
    if (!sopCommonModule) {
      return;
    }
    return sopCommonModule.sopInstanceUID === sopInstanceUid;
  });
}

/**
 * getImageIdOfReferencedFrame - Returns the imageId corresponding to the
 * specified sopInstanceUid and frameNumber for multi-frame images.
 *
 * @param  {String} sopInstanceUid   The sopInstanceUid of the desired image.
 * @param  {Number} frameNumber      The frame number.
 * @param  {String} imageIds         The list of imageIds.
 * @param  {Object} metadataProvider The metadataProvider to obtain sopInstanceUids
 *                                   from the cornerstone imageIds.
 * @return {String}                  The imageId that corresponds to the sopInstanceUid.
 */
function getImageIdOfReferencedFrame$1(sopInstanceUid, frameNumber, imageIds, metadataProvider) {
  var imageId = imageIds.find(function (imageId) {
    var sopCommonModule = metadataProvider.get("sopCommonModule", imageId);
    if (!sopCommonModule) {
      return;
    }
    var imageIdFrameNumber = Number(imageId.split("frame=")[1]);
    return (
      //frameNumber is zero indexed for cornerstoneDICOMImageLoader image Ids.
      sopCommonModule.sopInstanceUID === sopInstanceUid && imageIdFrameNumber === frameNumber - 1
    );
  });
  return imageId;
}

/**
 * getValidOrientations - returns an array of valid orientations.
 *
 * @param  iop - The row (0..2) an column (3..5) direction cosines.
 * @return  An array of valid orientations.
 */
function getValidOrientations$1(iop) {
  var orientations = [];

  // [0,  1,  2]: 0,   0hf,   0vf
  // [3,  4,  5]: 90,  90hf,  90vf
  // [6, 7]:      180, 270

  orientations[0] = iop;
  orientations[1] = flipIOP$1.h(iop);
  orientations[2] = flipIOP$1.v(iop);
  var iop90 = rotateDirectionCosinesInPlane$1(iop, Math.PI / 2);
  orientations[3] = iop90;
  orientations[4] = flipIOP$1.h(iop90);
  orientations[5] = flipIOP$1.v(iop90);
  orientations[6] = rotateDirectionCosinesInPlane$1(iop, Math.PI);
  orientations[7] = rotateDirectionCosinesInPlane$1(iop, 1.5 * Math.PI);
  return orientations;
}

/**
 * alignPixelDataWithSourceData -
 *
 * @param pixelData2D - The data to align.
 * @param iop - The orientation of the image slice.
 * @param orientations - An array of valid imageOrientationPatient values.
 * @return The aligned pixelData.
 */
function alignPixelDataWithSourceData$1(pixelData2D, iop, orientations) {
  if (compareIOP(iop, orientations[0])) {
    //Same orientation.
    return pixelData2D;
  } else if (compareIOP(iop, orientations[1])) {
    //Flipped vertically.
    return flipMatrix2D$1.v(pixelData2D);
  } else if (compareIOP(iop, orientations[2])) {
    //Flipped horizontally.
    return flipMatrix2D$1.h(pixelData2D);
  } else if (compareIOP(iop, orientations[3])) {
    //Rotated 90 degrees.
    return rotateMatrix902D$1(pixelData2D);
  } else if (compareIOP(iop, orientations[4])) {
    //Rotated 90 degrees and fliped horizontally.
    return flipMatrix2D$1.h(rotateMatrix902D$1(pixelData2D));
  } else if (compareIOP(iop, orientations[5])) {
    //Rotated 90 degrees and fliped vertically.
    return flipMatrix2D$1.v(rotateMatrix902D$1(pixelData2D));
  } else if (compareIOP(iop, orientations[6])) {
    //Rotated 180 degrees. // TODO -> Do this more effeciently, there is a 1:1 mapping like 90 degree rotation.
    return rotateMatrix902D$1(rotateMatrix902D$1(pixelData2D));
  } else if (compareIOP(iop, orientations[7])) {
    //Rotated 270 degrees.  // TODO -> Do this more effeciently, there is a 1:1 mapping like 90 degree rotation.
    return rotateMatrix902D$1(rotateMatrix902D$1(rotateMatrix902D$1(pixelData2D)));
  }
}
var dx = 1e-5;

/**
 * compareIOP - Returns true if iop1 and iop2 are equal
 * within a tollerance, dx.
 *
 * @param  iop1 - An ImageOrientationPatient array.
 * @param  iop2 - An ImageOrientationPatient array.
 * @return True if iop1 and iop2 are equal.
 */
function compareIOP(iop1, iop2) {
  return Math.abs(iop1[0] - iop2[0]) < dx && Math.abs(iop1[1] - iop2[1]) < dx && Math.abs(iop1[2] - iop2[2]) < dx && Math.abs(iop1[3] - iop2[3]) < dx && Math.abs(iop1[4] - iop2[4]) < dx && Math.abs(iop1[5] - iop2[5]) < dx;
}
function getSegmentMetadata$1(multiframe) {
  var data = [];
  var segmentSequence = multiframe.SegmentSequence;
  if (Array.isArray(segmentSequence)) {
    for (var segIdx = 0; segIdx < segmentSequence.length; segIdx++) {
      data.push(segmentSequence[segIdx]);
    }
  } else {
    // Only one segment, will be stored as an object.
    data.push(segmentSequence);
  }
  return {
    seriesInstanceUid: multiframe.ReferencedSeriesSequence.SeriesInstanceUID,
    data: data
  };
}

/**
 * Cornerstone adapters events
 */
var Events;
(function (Events) {
    /**
     * Cornerstone segmentation load progress event
     */
    Events["SEGMENTATION_LOAD_PROGRESS"] = "CORNERSTONE_ADAPTER_SEGMENTATION_LOAD_PROGRESS";
})(Events || (Events = {}));
var Events$1 = Events;

var index = /*#__PURE__*/Object.freeze({
  __proto__: null,
  Events: Events$1
});

var _utilities$orientatio = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.orientation,
  rotateDirectionCosinesInPlane = _utilities$orientatio.rotateDirectionCosinesInPlane,
  flipIOP = _utilities$orientatio.flipImageOrientationPatient,
  flipMatrix2D = _utilities$orientatio.flipMatrix2D,
  rotateMatrix902D = _utilities$orientatio.rotateMatrix902D,
  nearlyEqual = _utilities$orientatio.nearlyEqual;
var BitArray$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .data */ .aT.BitArray,
  DicomMessage = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .data */ .aT.DicomMessage,
  DicomMetaDictionary$2 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .data */ .aT.DicomMetaDictionary;
var Normalizer$2 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .normalizers */ .oq.Normalizer;
var SegmentationDerivation$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .derivations */ .U7.Segmentation;
var _utilities$compressio = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.compression,
  encode = _utilities$compressio.encode,
  decode = _utilities$compressio.decode;

/**
 *
 * @typedef {Object} BrushData
 * @property {Object} toolState - The cornerstoneTools global toolState.
 * @property {Object[]} segments - The cornerstoneTools segment metadata that corresponds to the
 *                                 seriesInstanceUid.
 */
var generateSegmentationDefaultOptions = {
  includeSliceSpacing: true,
  rleEncode: false
};

/**
 * generateSegmentation - Generates cornerstoneTools brush data, given a stack of
 * imageIds, images and the cornerstoneTools brushData.
 *
 * @param  {object[]} images An array of cornerstone images that contain the source
 *                           data under `image.data.byteArray.buffer`.
 * @param  {Object|Object[]} inputLabelmaps3D The cornerstone `Labelmap3D` object, or an array of objects.
 * @param  {Object} userOptions Options to pass to the segmentation derivation and `fillSegmentation`.
 * @returns {Blob}
 */
function generateSegmentation$2(images, inputLabelmaps3D) {
  var userOptions = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
  var isMultiframe = images[0].imageId.includes("?frame");
  var segmentation = _createSegFromImages(images, isMultiframe, userOptions);
  return fillSegmentation$1(segmentation, inputLabelmaps3D, userOptions);
}

/**
 * Fills a given segmentation object with data from the input labelmaps3D
 *
 * @param segmentation - The segmentation object to be filled.
 * @param inputLabelmaps3D - An array of 3D labelmaps, or a single 3D labelmap.
 * @param userOptions - Optional configuration settings. Will override the default options.
 *
 * @returns {object} The filled segmentation object.
 */
function fillSegmentation$1(segmentation, inputLabelmaps3D) {
  var userOptions = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {};
  var options = Object.assign({}, generateSegmentationDefaultOptions, userOptions);

  // Use another variable so we don't redefine labelmaps3D.
  var labelmaps3D = Array.isArray(inputLabelmaps3D) ? inputLabelmaps3D : [inputLabelmaps3D];
  var numberOfFrames = 0;
  var referencedFramesPerLabelmap = [];
  var _loop = function _loop() {
    var labelmap3D = labelmaps3D[labelmapIndex];
    var labelmaps2D = labelmap3D.labelmaps2D,
      metadata = labelmap3D.metadata;
    var referencedFramesPerSegment = [];
    for (var i = 1; i < metadata.length; i++) {
      if (metadata[i]) {
        referencedFramesPerSegment[i] = [];
      }
    }
    var _loop2 = function _loop2(_i) {
      var labelmap2D = labelmaps2D[_i];
      if (labelmaps2D[_i]) {
        var segmentsOnLabelmap = labelmap2D.segmentsOnLabelmap;
        segmentsOnLabelmap.forEach(function (segmentIndex) {
          if (segmentIndex !== 0) {
            referencedFramesPerSegment[segmentIndex].push(_i);
            numberOfFrames++;
          }
        });
      }
    };
    for (var _i = 0; _i < labelmaps2D.length; _i++) {
      _loop2(_i);
    }
    referencedFramesPerLabelmap[labelmapIndex] = referencedFramesPerSegment;
  };
  for (var labelmapIndex = 0; labelmapIndex < labelmaps3D.length; labelmapIndex++) {
    _loop();
  }
  segmentation.setNumberOfFrames(numberOfFrames);
  for (var _labelmapIndex = 0; _labelmapIndex < labelmaps3D.length; _labelmapIndex++) {
    var referencedFramesPerSegment = referencedFramesPerLabelmap[_labelmapIndex];
    var labelmap3D = labelmaps3D[_labelmapIndex];
    var metadata = labelmap3D.metadata;
    for (var segmentIndex = 1; segmentIndex < referencedFramesPerSegment.length; segmentIndex++) {
      var referencedFrameIndicies = referencedFramesPerSegment[segmentIndex];
      if (referencedFrameIndicies) {
        // Frame numbers start from 1.
        var referencedFrameNumbers = referencedFrameIndicies.map(function (element) {
          return element + 1;
        });
        var segmentMetadata = metadata[segmentIndex];
        var labelmaps = _getLabelmapsFromReferencedFrameIndicies(labelmap3D, referencedFrameIndicies);
        segmentation.addSegmentFromLabelmap(segmentMetadata, labelmaps, segmentIndex, referencedFrameNumbers);
      }
    }
  }
  if (options.rleEncode) {
    var rleEncodedFrames = encode(segmentation.dataset.PixelData, numberOfFrames, segmentation.dataset.Rows, segmentation.dataset.Columns);

    // Must use fractional now to RLE encode, as the DICOM standard only allows BitStored && BitsAllocated
    // to be 1 for BINARY. This is not ideal and there should be a better format for compression in this manner
    // added to the standard.
    segmentation.assignToDataset({
      BitsAllocated: "8",
      BitsStored: "8",
      HighBit: "7",
      SegmentationType: "FRACTIONAL",
      SegmentationFractionalType: "PROBABILITY",
      MaximumFractionalValue: "255"
    });
    segmentation.dataset._meta.TransferSyntaxUID = {
      Value: ["1.2.840.10008.1.2.5"],
      vr: "UI"
    };
    segmentation.dataset._vrMap.PixelData = "OB";
    segmentation.dataset.PixelData = rleEncodedFrames;
  } else {
    // If no rleEncoding, at least bitpack the data.
    segmentation.bitPackPixelData();
  }
  return segmentation;
}
function _getLabelmapsFromReferencedFrameIndicies(labelmap3D, referencedFrameIndicies) {
  var labelmaps2D = labelmap3D.labelmaps2D;
  var labelmaps = [];
  for (var i = 0; i < referencedFrameIndicies.length; i++) {
    var frame = referencedFrameIndicies[i];
    labelmaps.push(labelmaps2D[frame].pixelData);
  }
  return labelmaps;
}

/**
 * _createSegFromImages - description
 *
 * @param  {Object[]} images    An array of the cornerstone image objects.
 * @param  {Boolean} isMultiframe Whether the images are multiframe.
 * @returns {Object}              The Seg derived dataSet.
 */
function _createSegFromImages(images, isMultiframe, options) {
  var datasets = [];
  if (isMultiframe) {
    var image = images[0];
    var arrayBuffer = image.data.byteArray.buffer;
    var dicomData = DicomMessage.readFile(arrayBuffer);
    var dataset = DicomMetaDictionary$2.naturalizeDataset(dicomData.dict);
    dataset._meta = DicomMetaDictionary$2.namifyDataset(dicomData.meta);
    datasets.push(dataset);
  } else {
    for (var i = 0; i < images.length; i++) {
      var _image = images[i];
      var _arrayBuffer = _image.data.byteArray.buffer;
      var _dicomData = DicomMessage.readFile(_arrayBuffer);
      var _dataset = DicomMetaDictionary$2.naturalizeDataset(_dicomData.dict);
      _dataset._meta = DicomMetaDictionary$2.namifyDataset(_dicomData.meta);
      datasets.push(_dataset);
    }
  }
  var multiframe = Normalizer$2.normalizeToDataset(datasets);
  return new SegmentationDerivation$1([multiframe], options);
}

/**
 * generateToolState - Given a set of cornrstoneTools imageIds and a Segmentation buffer,
 * derive cornerstoneTools toolState and brush metadata.
 *
 * @param  {string[]} imageIds - An array of the imageIds.
 * @param  {ArrayBuffer} arrayBuffer - The SEG arrayBuffer.
 * @param  {*} metadataProvider.
 * @param  {obj} options - Options object.
 *
 * @return {[]ArrayBuffer}a list of array buffer for each labelMap
 * @return {Object} an object from which the segment metadata can be derived
 * @return {[][][]} 2D list containing the track of segments per frame
 * @return {[][][]} 3D list containing the track of segments per frame for each labelMap
 *                  (available only for the overlapping case).
 */
function generateToolState$2(_x, _x2, _x3, _x4) {
  return _generateToolState.apply(this, arguments);
} // function insertPixelDataPerpendicular(
//     segmentsOnFrame,
//     labelmapBuffer,
//     pixelData,
//     multiframe,
//     imageIds,
//     validOrientations,
//     metadataProvider
// ) {
//     const {
//         SharedFunctionalGroupsSequence,
//         PerFrameFunctionalGroupsSequence,
//         Rows,
//         Columns
//     } = multiframe;
//     const firstImagePlaneModule = metadataProvider.get(
//         "imagePlaneModule",
//         imageIds[0]
//     );
//     const lastImagePlaneModule = metadataProvider.get(
//         "imagePlaneModule",
//         imageIds[imageIds.length - 1]
//     );
//     console.log(firstImagePlaneModule);
//     console.log(lastImagePlaneModule);
//     const corners = [
//         ...getCorners(firstImagePlaneModule),
//         ...getCorners(lastImagePlaneModule)
//     ];
//     console.log(`corners:`);
//     console.log(corners);
//     const indexToWorld = mat4.create();
//     const ippFirstFrame = firstImagePlaneModule.imagePositionPatient;
//     const rowCosines = Array.isArray(firstImagePlaneModule.rowCosines)
//         ? [...firstImagePlaneModule.rowCosines]
//         : [
//               firstImagePlaneModule.rowCosines.x,
//               firstImagePlaneModule.rowCosines.y,
//               firstImagePlaneModule.rowCosines.z
//           ];
//     const columnCosines = Array.isArray(firstImagePlaneModule.columnCosines)
//         ? [...firstImagePlaneModule.columnCosines]
//         : [
//               firstImagePlaneModule.columnCosines.x,
//               firstImagePlaneModule.columnCosines.y,
//               firstImagePlaneModule.columnCosines.z
//           ];
//     const { pixelSpacing } = firstImagePlaneModule;
//     mat4.set(
//         indexToWorld,
//         // Column 1
//         0,
//         0,
//         0,
//         ippFirstFrame[0],
//         // Column 2
//         0,
//         0,
//         0,
//         ippFirstFrame[1],
//         // Column 3
//         0,
//         0,
//         0,
//         ippFirstFrame[2],
//         // Column 4
//         0,
//         0,
//         0,
//         1
//     );
//     // TODO -> Get origin and (x,y,z) increments to build a translation matrix:
//     // TODO -> Equation C.7.6.2.1-1
//     // | cx*di rx* Xx 0 |  |x|
//     // | cy*di ry Xy 0 |  |y|
//     // | cz*di rz Xz 0 |  |z|
//     // | tx ty tz 1 |  |1|
//     // const [
//     //     0, 0 , 0 , 0,
//     //     0, 0 , 0 , 0,
//     //     0, 0 , 0 , 0,
//     //     ipp[0], ipp[1] , ipp[2] , 1,
//     // ]
//     // Each frame:
//     // Find which corner the first voxel lines up with (one of 8 corners.)
//     // Find how i,j,k orient with respect to source volume.
//     // Go through each frame, find location in source to start, and whether to increment +/ix,+/-y,+/-z
//     //   through each voxel.
//     // [1,0,0,0,1,0]
//     // const [
//     // ]
//     // Invert transformation matrix to get worldToIndex
//     // Apply world to index on each point to fill up the matrix.
//     // const sharedImageOrientationPatient = SharedFunctionalGroupsSequence.PlaneOrientationSequence
//     //     ? SharedFunctionalGroupsSequence.PlaneOrientationSequence
//     //           .ImageOrientationPatient
//     //     : undefined;
//     // const sliceLength = Columns * Rows;
// }
// function getCorners(imagePlaneModule) {
//     // console.log(imagePlaneModule);
//     const {
//         rows,
//         columns,
//         rowCosines,
//         columnCosines,
//         imagePositionPatient: ipp,
//         rowPixelSpacing,
//         columnPixelSpacing
//     } = imagePlaneModule;
//     const rowLength = columns * columnPixelSpacing;
//     const columnLength = rows * rowPixelSpacing;
//     const entireRowVector = [
//         rowLength * columnCosines[0],
//         rowLength * columnCosines[1],
//         rowLength * columnCosines[2]
//     ];
//     const entireColumnVector = [
//         columnLength * rowCosines[0],
//         columnLength * rowCosines[1],
//         columnLength * rowCosines[2]
//     ];
//     const topLeft = [ipp[0], ipp[1], ipp[2]];
//     const topRight = [
//         topLeft[0] + entireRowVector[0],
//         topLeft[1] + entireRowVector[1],
//         topLeft[2] + entireRowVector[2]
//     ];
//     const bottomLeft = [
//         topLeft[0] + entireColumnVector[0],
//         topLeft[1] + entireColumnVector[1],
//         topLeft[2] + entireColumnVector[2]
//     ];
//     const bottomRight = [
//         bottomLeft[0] + entireRowVector[0],
//         bottomLeft[1] + entireRowVector[1],
//         bottomLeft[2] + entireRowVector[2]
//     ];
//     return [topLeft, topRight, bottomLeft, bottomRight];
// }
/**
 * Find the reference frame of the segmentation frame in the source data.
 *
 * @param  {Object}      multiframe        dicom metadata
 * @param  {Int}         frameSegment      frame dicom index
 * @param  {String[]}    imageIds          A list of imageIds.
 * @param  {Object}      sopUIDImageIdIndexMap  A map of SOPInstanceUID to imageId
 * @param  {Float}       tolerance         The tolerance parameter
 *
 * @returns {String}     Returns the imageId
 */
function _generateToolState() {
  _generateToolState = _asyncToGenerator( /*#__PURE__*/_regeneratorRuntime().mark(function _callee(imageIds, arrayBuffer, metadataProvider, options) {
    var _options$skipOverlapp, skipOverlapping, _options$tolerance, tolerance, _options$TypedArrayCo, TypedArrayConstructor, _options$maxBytesPerC, maxBytesPerChunk, eventTarget, triggerEvent, dicomData, dataset, multiframe, imagePlaneModule, generalSeriesModule, SeriesInstanceUID, ImageOrientationPatient, validOrientations, sliceLength, segMetadata, TransferSyntaxUID, pixelData, pixelDataChunks, rleEncodedFrames, orientation, sopUIDImageIdIndexMap, overlapping, insertFunction, segmentsOnFrameArray, segmentsOnFrame, arrayBufferLength, labelmapBufferArray, imageIdMaps, segmentsPixelIndices, centroidXYZ;
    return _regeneratorRuntime().wrap(function _callee$(_context) {
      while (1) switch (_context.prev = _context.next) {
        case 0:
          _options$skipOverlapp = options.skipOverlapping, skipOverlapping = _options$skipOverlapp === void 0 ? false : _options$skipOverlapp, _options$tolerance = options.tolerance, tolerance = _options$tolerance === void 0 ? 1e-3 : _options$tolerance, _options$TypedArrayCo = options.TypedArrayConstructor, TypedArrayConstructor = _options$TypedArrayCo === void 0 ? Uint8Array : _options$TypedArrayCo, _options$maxBytesPerC = options.maxBytesPerChunk, maxBytesPerChunk = _options$maxBytesPerC === void 0 ? 199000000 : _options$maxBytesPerC, eventTarget = options.eventTarget, triggerEvent = options.triggerEvent;
          dicomData = DicomMessage.readFile(arrayBuffer);
          dataset = DicomMetaDictionary$2.naturalizeDataset(dicomData.dict);
          dataset._meta = DicomMetaDictionary$2.namifyDataset(dicomData.meta);
          multiframe = Normalizer$2.normalizeToDataset([dataset]);
          imagePlaneModule = metadataProvider.get("imagePlaneModule", imageIds[0]);
          generalSeriesModule = metadataProvider.get("generalSeriesModule", imageIds[0]);
          SeriesInstanceUID = generalSeriesModule.seriesInstanceUID;
          if (!imagePlaneModule) {
            console.warn("Insufficient metadata, imagePlaneModule missing.");
          }
          ImageOrientationPatient = Array.isArray(imagePlaneModule.rowCosines) ? [].concat(_toConsumableArray(imagePlaneModule.rowCosines), _toConsumableArray(imagePlaneModule.columnCosines)) : [imagePlaneModule.rowCosines.x, imagePlaneModule.rowCosines.y, imagePlaneModule.rowCosines.z, imagePlaneModule.columnCosines.x, imagePlaneModule.columnCosines.y, imagePlaneModule.columnCosines.z]; // Get IOP from ref series, compute supported orientations:
          validOrientations = getValidOrientations(ImageOrientationPatient);
          sliceLength = multiframe.Columns * multiframe.Rows;
          segMetadata = getSegmentMetadata(multiframe, SeriesInstanceUID);
          TransferSyntaxUID = multiframe._meta.TransferSyntaxUID.Value[0];
          if (!(TransferSyntaxUID === "1.2.840.10008.1.2.5")) {
            _context.next = 23;
            break;
          }
          rleEncodedFrames = Array.isArray(multiframe.PixelData) ? multiframe.PixelData : [multiframe.PixelData];
          pixelData = decode(rleEncodedFrames, multiframe.Rows, multiframe.Columns);
          if (!(multiframe.BitsStored === 1)) {
            _context.next = 20;
            break;
          }
          console.warn("No implementation for rle + bitbacking.");
          return _context.abrupt("return");
        case 20:
          // Todo: need to test this with rle data
          pixelDataChunks = [pixelData];
          _context.next = 26;
          break;
        case 23:
          pixelDataChunks = unpackPixelData(multiframe, {
            maxBytesPerChunk: maxBytesPerChunk
          });
          if (pixelDataChunks) {
            _context.next = 26;
            break;
          }
          throw new Error("Fractional segmentations are not yet supported");
        case 26:
          orientation = checkOrientation(multiframe, validOrientations, [imagePlaneModule.rows, imagePlaneModule.columns, imageIds.length], tolerance); // Pre-compute the sop UID to imageId index map so that in the for loop
          // we don't have to call metadataProvider.get() for each imageId over
          // and over again.
          sopUIDImageIdIndexMap = imageIds.reduce(function (acc, imageId) {
            var _metadataProvider$get = metadataProvider.get("generalImageModule", imageId),
              sopInstanceUid = _metadataProvider$get.sopInstanceUid;
            acc[sopInstanceUid] = imageId;
            return acc;
          }, {});
          overlapping = false;
          if (!skipOverlapping) {
            overlapping = checkSEGsOverlapping(pixelDataChunks, multiframe, imageIds, validOrientations, metadataProvider, tolerance, TypedArrayConstructor, sopUIDImageIdIndexMap);
          }
          _context.t0 = orientation;
          _context.next = _context.t0 === "Planar" ? 33 : _context.t0 === "Perpendicular" ? 35 : _context.t0 === "Oblique" ? 36 : 37;
          break;
        case 33:
          if (overlapping) {
            insertFunction = insertOverlappingPixelDataPlanar;
          } else {
            insertFunction = insertPixelDataPlanar;
          }
          return _context.abrupt("break", 37);
        case 35:
          throw new Error("Segmentations orthogonal to the acquisition plane of the source data are not yet supported.");
        case 36:
          throw new Error("Segmentations oblique to the acquisition plane of the source data are not yet supported.");
        case 37:
          /* if SEGs are overlapping:
          1) the labelmapBuffer will contain M volumes which have non-overlapping segments;
          2) segmentsOnFrame will have M * numberOfFrames values to track in which labelMap are the segments;
          3) insertFunction will return the number of LabelMaps
          4) generateToolState return is an array*/
          segmentsOnFrameArray = [];
          segmentsOnFrameArray[0] = [];
          segmentsOnFrame = [];
          arrayBufferLength = sliceLength * imageIds.length * TypedArrayConstructor.BYTES_PER_ELEMENT;
          labelmapBufferArray = [];
          labelmapBufferArray[0] = new ArrayBuffer(arrayBufferLength);

          // Precompute the indices and metadata so that we don't have to call
          // a function for each imageId in the for loop.
          imageIdMaps = imageIds.reduce(function (acc, curr, index) {
            acc.indices[curr] = index;
            acc.metadata[curr] = metadataProvider.get("instance", curr);
            return acc;
          }, {
            indices: {},
            metadata: {}
          }); // This is the centroid calculation for each segment Index, the data structure
          // is a Map with key = segmentIndex and value = {imageIdIndex: centroid, ...}
          // later on we will use this data structure to calculate the centroid of the
          // segment in the labelmapBuffer
          segmentsPixelIndices = new Map();
          _context.next = 47;
          return insertFunction(segmentsOnFrame, segmentsOnFrameArray, labelmapBufferArray, pixelDataChunks, multiframe, imageIds, validOrientations, metadataProvider, tolerance, TypedArrayConstructor, segmentsPixelIndices, sopUIDImageIdIndexMap, imageIdMaps, eventTarget, triggerEvent);
        case 47:
          // calculate the centroid of each segment
          centroidXYZ = new Map();
          segmentsPixelIndices.forEach(function (imageIdIndexBufferIndex, segmentIndex) {
            var _calculateCentroid = calculateCentroid(imageIdIndexBufferIndex, multiframe),
              xAcc = _calculateCentroid.xAcc,
              yAcc = _calculateCentroid.yAcc,
              zAcc = _calculateCentroid.zAcc,
              count = _calculateCentroid.count;
            centroidXYZ.set(segmentIndex, {
              x: Math.floor(xAcc / count),
              y: Math.floor(yAcc / count),
              z: Math.floor(zAcc / count)
            });
          });
          return _context.abrupt("return", {
            labelmapBufferArray: labelmapBufferArray,
            segMetadata: segMetadata,
            segmentsOnFrame: segmentsOnFrame,
            segmentsOnFrameArray: segmentsOnFrameArray,
            centroids: centroidXYZ
          });
        case 50:
        case "end":
          return _context.stop();
      }
    }, _callee);
  }));
  return _generateToolState.apply(this, arguments);
}
function findReferenceSourceImageId(multiframe, frameSegment, imageIds, metadataProvider, tolerance, sopUIDImageIdIndexMap) {
  var imageId = undefined;
  if (!multiframe) {
    return imageId;
  }
  var FrameOfReferenceUID = multiframe.FrameOfReferenceUID,
    PerFrameFunctionalGroupsSequence = multiframe.PerFrameFunctionalGroupsSequence,
    SourceImageSequence = multiframe.SourceImageSequence,
    ReferencedSeriesSequence = multiframe.ReferencedSeriesSequence;
  if (!PerFrameFunctionalGroupsSequence || PerFrameFunctionalGroupsSequence.length === 0) {
    return imageId;
  }
  var PerFrameFunctionalGroup = PerFrameFunctionalGroupsSequence[frameSegment];
  if (!PerFrameFunctionalGroup) {
    return imageId;
  }
  var frameSourceImageSequence = undefined;
  if (SourceImageSequence && SourceImageSequence.length !== 0) {
    frameSourceImageSequence = SourceImageSequence[frameSegment];
  } else if (PerFrameFunctionalGroup.DerivationImageSequence) {
    var DerivationImageSequence = PerFrameFunctionalGroup.DerivationImageSequence;
    if (Array.isArray(DerivationImageSequence)) {
      if (DerivationImageSequence.length !== 0) {
        DerivationImageSequence = DerivationImageSequence[0];
      } else {
        DerivationImageSequence = undefined;
      }
    }
    if (DerivationImageSequence) {
      frameSourceImageSequence = DerivationImageSequence.SourceImageSequence;
      if (Array.isArray(frameSourceImageSequence)) {
        if (frameSourceImageSequence.length !== 0) {
          frameSourceImageSequence = frameSourceImageSequence[0];
        } else {
          frameSourceImageSequence = undefined;
        }
      }
    }
  }
  if (frameSourceImageSequence) {
    imageId = getImageIdOfSourceImageBySourceImageSequence(frameSourceImageSequence, sopUIDImageIdIndexMap);
  }
  if (imageId === undefined && ReferencedSeriesSequence) {
    var referencedSeriesSequence = Array.isArray(ReferencedSeriesSequence) ? ReferencedSeriesSequence[0] : ReferencedSeriesSequence;
    var ReferencedSeriesInstanceUID = referencedSeriesSequence.SeriesInstanceUID;
    imageId = getImageIdOfSourceImagebyGeometry(ReferencedSeriesInstanceUID, FrameOfReferenceUID, PerFrameFunctionalGroup, imageIds, metadataProvider, tolerance);
  }
  return imageId;
}

/**
 * Checks if there is any overlapping segmentations.
 *  @returns {boolean} Returns a flag if segmentations overlapping
 */

function checkSEGsOverlapping(pixelData, multiframe, imageIds, validOrientations, metadataProvider, tolerance, TypedArrayConstructor, sopUIDImageIdIndexMap) {
  var SharedFunctionalGroupsSequence = multiframe.SharedFunctionalGroupsSequence,
    PerFrameFunctionalGroupsSequence = multiframe.PerFrameFunctionalGroupsSequence,
    SegmentSequence = multiframe.SegmentSequence,
    Rows = multiframe.Rows,
    Columns = multiframe.Columns;
  var numberOfSegs = SegmentSequence.length;
  if (numberOfSegs < 2) {
    return false;
  }
  var sharedImageOrientationPatient = SharedFunctionalGroupsSequence.PlaneOrientationSequence ? SharedFunctionalGroupsSequence.PlaneOrientationSequence.ImageOrientationPatient : undefined;
  var sliceLength = Columns * Rows;
  var groupsLen = PerFrameFunctionalGroupsSequence.length;

  /** sort groupsLen to have all the segments for each frame in an array
   * frame 2 : 1, 2
   * frame 4 : 1, 3
   * frame 5 : 4
   */

  var frameSegmentsMapping = new Map();
  var _loop3 = function _loop3() {
    var segmentIndex = getSegmentIndex(multiframe, frameSegment);
    if (segmentIndex === undefined) {
      console.warn("Could not retrieve the segment index for frame segment " + frameSegment + ", skipping this frame.");
      return "continue";
    }
    var imageId = findReferenceSourceImageId(multiframe, frameSegment, imageIds, metadataProvider, tolerance, sopUIDImageIdIndexMap);
    if (!imageId) {
      console.warn("Image not present in stack, can't import frame : " + frameSegment + ".");
      return "continue";
    }
    var imageIdIndex = imageIds.findIndex(function (element) {
      return element === imageId;
    });
    if (frameSegmentsMapping.has(imageIdIndex)) {
      var segmentArray = frameSegmentsMapping.get(imageIdIndex);
      if (!segmentArray.includes(frameSegment)) {
        segmentArray.push(frameSegment);
        frameSegmentsMapping.set(imageIdIndex, segmentArray);
      }
    } else {
      frameSegmentsMapping.set(imageIdIndex, [frameSegment]);
    }
  };
  for (var frameSegment = 0; frameSegment < groupsLen; ++frameSegment) {
    var _ret = _loop3();
    if (_ret === "continue") continue;
  }
  var _iterator = _createForOfIteratorHelper(frameSegmentsMapping.entries()),
    _step;
  try {
    for (_iterator.s(); !(_step = _iterator.n()).done;) {
      var _step$value = _slicedToArray(_step.value, 2),
        role = _step$value[1];
      var temp2DArray = new TypedArrayConstructor(sliceLength).fill(0);
      for (var i = 0; i < role.length; ++i) {
        var _frameSegment = role[i];
        var PerFrameFunctionalGroups = PerFrameFunctionalGroupsSequence[_frameSegment];
        var ImageOrientationPatientI = sharedImageOrientationPatient || PerFrameFunctionalGroups.PlaneOrientationSequence.ImageOrientationPatient;
        var view = readFromUnpackedChunks(pixelData, _frameSegment * sliceLength, sliceLength);
        var pixelDataI2D = ndarray__WEBPACK_IMPORTED_MODULE_2___default()(view, [Rows, Columns]);
        var alignedPixelDataI = alignPixelDataWithSourceData(pixelDataI2D, ImageOrientationPatientI, validOrientations, tolerance);
        if (!alignedPixelDataI) {
          console.warn("Individual SEG frames are out of plane with respect to the first SEG frame, this is not yet supported, skipping this frame.");
          continue;
        }
        var data = alignedPixelDataI.data;
        for (var j = 0, len = data.length; j < len; ++j) {
          if (data[j] !== 0) {
            temp2DArray[j]++;
            if (temp2DArray[j] > 1) {
              return true;
            }
          }
        }
      }
    }
  } catch (err) {
    _iterator.e(err);
  } finally {
    _iterator.f();
  }
  return false;
}
function insertOverlappingPixelDataPlanar(segmentsOnFrame, segmentsOnFrameArray, labelmapBufferArray, pixelData, multiframe, imageIds, validOrientations, metadataProvider, tolerance, TypedArrayConstructor, segmentsPixelIndices, sopUIDImageIdIndexMap) {
  var SharedFunctionalGroupsSequence = multiframe.SharedFunctionalGroupsSequence,
    PerFrameFunctionalGroupsSequence = multiframe.PerFrameFunctionalGroupsSequence,
    Rows = multiframe.Rows,
    Columns = multiframe.Columns;
  var sharedImageOrientationPatient = SharedFunctionalGroupsSequence.PlaneOrientationSequence ? SharedFunctionalGroupsSequence.PlaneOrientationSequence.ImageOrientationPatient : undefined;
  var sliceLength = Columns * Rows;
  var arrayBufferLength = sliceLength * imageIds.length * TypedArrayConstructor.BYTES_PER_ELEMENT;
  // indicate the number of labelMaps
  var M = 1;

  // indicate the current labelMap array index;
  var m = 0;

  // temp array for checking overlaps
  var tempBuffer = labelmapBufferArray[m].slice(0);

  // temp list for checking overlaps
  var tempSegmentsOnFrame = lodash_clonedeep__WEBPACK_IMPORTED_MODULE_3___default()(segmentsOnFrameArray[m]);

  /** split overlapping SEGs algorithm for each segment:
   *  A) copy the labelmapBuffer in the array with index 0
   *  B) add the segment pixel per pixel on the copied buffer from (A)
   *  C) if no overlap, copy the results back on the orignal array from (A)
   *  D) if overlap, repeat increasing the index m up to M (if out of memory, add new buffer in the array and M++);
   */

  var numberOfSegs = multiframe.SegmentSequence.length;
  for (var segmentIndexToProcess = 1; segmentIndexToProcess <= numberOfSegs; ++segmentIndexToProcess) {
    var _loop4 = function _loop4(_i2) {
      var PerFrameFunctionalGroups = PerFrameFunctionalGroupsSequence[_i2];
      var segmentIndex = getSegmentIndex(multiframe, _i2);
      if (segmentIndex === undefined) {
        throw new Error("Could not retrieve the segment index. Aborting segmentation loading.");
      }
      if (segmentIndex !== segmentIndexToProcess) {
        i = _i2;
        return "continue";
      }
      var ImageOrientationPatientI = sharedImageOrientationPatient || PerFrameFunctionalGroups.PlaneOrientationSequence.ImageOrientationPatient;

      // Since we moved to the chunks approach, we need to read the data
      // and handle scenarios where the portion of data is in one chunk
      // and the other portion is in another chunk
      var view = readFromUnpackedChunks(pixelData, _i2 * sliceLength, sliceLength);
      var pixelDataI2D = ndarray__WEBPACK_IMPORTED_MODULE_2___default()(view, [Rows, Columns]);
      var alignedPixelDataI = alignPixelDataWithSourceData(pixelDataI2D, ImageOrientationPatientI, validOrientations, tolerance);
      if (!alignedPixelDataI) {
        throw new Error("Individual SEG frames are out of plane with respect to the first SEG frame. " + "This is not yet supported. Aborting segmentation loading.");
      }
      var imageId = findReferenceSourceImageId(multiframe, _i2, imageIds, metadataProvider, tolerance, sopUIDImageIdIndexMap);
      if (!imageId) {
        console.warn("Image not present in stack, can't import frame : " + _i2 + ".");
        i = _i2;
        return "continue";
      }
      var sourceImageMetadata = metadataProvider.get("instance", imageId);
      if (Rows !== sourceImageMetadata.Rows || Columns !== sourceImageMetadata.Columns) {
        throw new Error("Individual SEG frames have different geometry dimensions (Rows and Columns) " + "respect to the source image reference frame. This is not yet supported. " + "Aborting segmentation loading. ");
      }
      var imageIdIndex = imageIds.findIndex(function (element) {
        return element === imageId;
      });
      var byteOffset = sliceLength * imageIdIndex * TypedArrayConstructor.BYTES_PER_ELEMENT;
      var labelmap2DView = new TypedArrayConstructor(tempBuffer, byteOffset, sliceLength);
      var data = alignedPixelDataI.data;
      var segmentOnFrame = false;
      for (var j = 0, len = alignedPixelDataI.data.length; j < len; ++j) {
        if (data[j]) {
          if (labelmap2DView[j] !== 0) {
            m++;
            if (m >= M) {
              labelmapBufferArray[m] = new ArrayBuffer(arrayBufferLength);
              segmentsOnFrameArray[m] = [];
              M++;
            }
            tempBuffer = labelmapBufferArray[m].slice(0);
            tempSegmentsOnFrame = lodash_clonedeep__WEBPACK_IMPORTED_MODULE_3___default()(segmentsOnFrameArray[m]);
            _i2 = 0;
            break;
          } else {
            labelmap2DView[j] = segmentIndex;
            segmentOnFrame = true;
          }
        }
      }
      if (segmentOnFrame) {
        if (!tempSegmentsOnFrame[imageIdIndex]) {
          tempSegmentsOnFrame[imageIdIndex] = [];
        }
        tempSegmentsOnFrame[imageIdIndex].push(segmentIndex);
        if (!segmentsOnFrame[imageIdIndex]) {
          segmentsOnFrame[imageIdIndex] = [];
        }
        segmentsOnFrame[imageIdIndex].push(segmentIndex);
      }
      i = _i2;
    };
    for (var i = 0, groupsLen = PerFrameFunctionalGroupsSequence.length; i < groupsLen; ++i) {
      var _ret2 = _loop4(i);
      if (_ret2 === "continue") continue;
    }
    labelmapBufferArray[m] = tempBuffer.slice(0);
    segmentsOnFrameArray[m] = lodash_clonedeep__WEBPACK_IMPORTED_MODULE_3___default()(tempSegmentsOnFrame);

    // reset temp variables/buffers for new segment
    m = 0;
    tempBuffer = labelmapBufferArray[m].slice(0);
    tempSegmentsOnFrame = lodash_clonedeep__WEBPACK_IMPORTED_MODULE_3___default()(segmentsOnFrameArray[m]);
  }
}
var getSegmentIndex = function getSegmentIndex(multiframe, frame) {
  var PerFrameFunctionalGroupsSequence = multiframe.PerFrameFunctionalGroupsSequence,
    SharedFunctionalGroupsSequence = multiframe.SharedFunctionalGroupsSequence;
  var PerFrameFunctionalGroups = PerFrameFunctionalGroupsSequence[frame];
  return PerFrameFunctionalGroups && PerFrameFunctionalGroups.SegmentIdentificationSequence ? PerFrameFunctionalGroups.SegmentIdentificationSequence.ReferencedSegmentNumber : SharedFunctionalGroupsSequence.SegmentIdentificationSequence ? SharedFunctionalGroupsSequence.SegmentIdentificationSequence.ReferencedSegmentNumber : undefined;
};
function insertPixelDataPlanar(segmentsOnFrame, segmentsOnFrameArray, labelmapBufferArray, pixelData, multiframe, imageIds, validOrientations, metadataProvider, tolerance, TypedArrayConstructor, segmentsPixelIndices, sopUIDImageIdIndexMap, imageIdMaps, eventTarget, triggerEvent) {
  var SharedFunctionalGroupsSequence = multiframe.SharedFunctionalGroupsSequence,
    PerFrameFunctionalGroupsSequence = multiframe.PerFrameFunctionalGroupsSequence,
    Rows = multiframe.Rows,
    Columns = multiframe.Columns;
  var sharedImageOrientationPatient = SharedFunctionalGroupsSequence.PlaneOrientationSequence ? SharedFunctionalGroupsSequence.PlaneOrientationSequence.ImageOrientationPatient : undefined;
  var sliceLength = Columns * Rows;
  var i = 0;
  var groupsLen = PerFrameFunctionalGroupsSequence.length;
  var chunkSize = Math.ceil(groupsLen / 10); // 10% of total length

  var shouldTriggerEvent = triggerEvent && eventTarget;

  // Below, we chunk the processing of the frames to avoid blocking the main thread
  // if the segmentation is large. We also use a promise to allow the caller to
  // wait for the processing to finish.
  return new Promise(function (resolve) {
    function processInChunks() {
      // process one chunk
      for (var end = Math.min(i + chunkSize, groupsLen); i < end; ++i) {
        var PerFrameFunctionalGroups = PerFrameFunctionalGroupsSequence[i];
        var ImageOrientationPatientI = sharedImageOrientationPatient || PerFrameFunctionalGroups.PlaneOrientationSequence.ImageOrientationPatient;
        var view = readFromUnpackedChunks(pixelData, i * sliceLength, sliceLength);
        var pixelDataI2D = ndarray__WEBPACK_IMPORTED_MODULE_2___default()(view, [Rows, Columns]);
        var alignedPixelDataI = alignPixelDataWithSourceData(pixelDataI2D, ImageOrientationPatientI, validOrientations, tolerance);
        if (!alignedPixelDataI) {
          throw new Error("Individual SEG frames are out of plane with respect to the first SEG frame. " + "This is not yet supported. Aborting segmentation loading.");
        }
        var segmentIndex = getSegmentIndex(multiframe, i);
        if (segmentIndex === undefined) {
          throw new Error("Could not retrieve the segment index. Aborting segmentation loading.");
        }
        if (!segmentsPixelIndices.has(segmentIndex)) {
          segmentsPixelIndices.set(segmentIndex, {});
        }
        var imageId = findReferenceSourceImageId(multiframe, i, imageIds, metadataProvider, tolerance, sopUIDImageIdIndexMap);
        if (!imageId) {
          console.warn("Image not present in stack, can't import frame : " + i + ".");
          continue;
        }
        var sourceImageMetadata = imageIdMaps.metadata[imageId];
        if (Rows !== sourceImageMetadata.Rows || Columns !== sourceImageMetadata.Columns) {
          throw new Error("Individual SEG frames have different geometry dimensions (Rows and Columns) " + "respect to the source image reference frame. This is not yet supported. " + "Aborting segmentation loading. ");
        }
        var imageIdIndex = imageIdMaps.indices[imageId];
        var byteOffset = sliceLength * imageIdIndex * TypedArrayConstructor.BYTES_PER_ELEMENT;
        var labelmap2DView = new TypedArrayConstructor(labelmapBufferArray[0], byteOffset, sliceLength);
        var data = alignedPixelDataI.data;
        var indexCache = [];
        for (var j = 0, len = alignedPixelDataI.data.length; j < len; ++j) {
          if (data[j]) {
            for (var x = j; x < len; ++x) {
              if (data[x]) {
                labelmap2DView[x] = segmentIndex;
                indexCache.push(x);
              }
            }
            if (!segmentsOnFrame[imageIdIndex]) {
              segmentsOnFrame[imageIdIndex] = [];
            }
            segmentsOnFrame[imageIdIndex].push(segmentIndex);
            break;
          }
        }
        var segmentIndexObject = segmentsPixelIndices.get(segmentIndex);
        segmentIndexObject[imageIdIndex] = indexCache;
        segmentsPixelIndices.set(segmentIndex, segmentIndexObject);
      }

      // trigger an event after each chunk
      if (shouldTriggerEvent) {
        var percentComplete = Math.round(i / groupsLen * 100);
        triggerEvent(eventTarget, Events$1.SEGMENTATION_LOAD_PROGRESS, {
          percentComplete: percentComplete
        });
      }

      // schedule next chunk
      if (i < groupsLen) {
        setTimeout(processInChunks, 0);
      } else {
        // resolve the Promise when all chunks have been processed
        resolve();
      }
    }
    processInChunks();
  });
}
function checkOrientation(multiframe, validOrientations, sourceDataDimensions, tolerance) {
  var SharedFunctionalGroupsSequence = multiframe.SharedFunctionalGroupsSequence,
    PerFrameFunctionalGroupsSequence = multiframe.PerFrameFunctionalGroupsSequence;
  var sharedImageOrientationPatient = SharedFunctionalGroupsSequence.PlaneOrientationSequence ? SharedFunctionalGroupsSequence.PlaneOrientationSequence.ImageOrientationPatient : undefined;

  // Check if in plane.
  var PerFrameFunctionalGroups = PerFrameFunctionalGroupsSequence[0];
  var iop = sharedImageOrientationPatient || PerFrameFunctionalGroups.PlaneOrientationSequence.ImageOrientationPatient;
  var inPlane = validOrientations.some(function (operation) {
    return compareArrays(iop, operation, tolerance);
  });
  if (inPlane) {
    return "Planar";
  }
  if (checkIfPerpendicular(iop, validOrientations[0], tolerance) && sourceDataDimensions.includes(multiframe.Rows) && sourceDataDimensions.includes(multiframe.Columns)) {
    // Perpendicular and fits on same grid.
    return "Perpendicular";
  }
  return "Oblique";
}

/**
 * checkIfPerpendicular - Returns true if iop1 and iop2 are perpendicular
 * within a tolerance.
 *
 * @param  {Number[6]} iop1 An ImageOrientationPatient array.
 * @param  {Number[6]} iop2 An ImageOrientationPatient array.
 * @param  {Number} tolerance.
 * @return {Boolean} True if iop1 and iop2 are equal.
 */
function checkIfPerpendicular(iop1, iop2, tolerance) {
  var absDotColumnCosines = Math.abs(iop1[0] * iop2[0] + iop1[1] * iop2[1] + iop1[2] * iop2[2]);
  var absDotRowCosines = Math.abs(iop1[3] * iop2[3] + iop1[4] * iop2[4] + iop1[5] * iop2[5]);
  return (absDotColumnCosines < tolerance || Math.abs(absDotColumnCosines - 1) < tolerance) && (absDotRowCosines < tolerance || Math.abs(absDotRowCosines - 1) < tolerance);
}

/**
 * unpackPixelData - Unpacks bit packed pixelData if the Segmentation is BINARY.
 *
 * @param  {Object} multiframe The multiframe dataset.
 * @param  {Object} options    Options for the unpacking.
 * @return {Uint8Array}      The unpacked pixelData.
 */
function unpackPixelData(multiframe, options) {
  var segType = multiframe.SegmentationType;
  var data;
  if (Array.isArray(multiframe.PixelData)) {
    data = multiframe.PixelData[0];
  } else {
    data = multiframe.PixelData;
  }
  if (data === undefined) {
    dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .log */ .cM.error("This segmentation pixeldata is undefined.");
  }
  if (segType === "BINARY") {
    // For extreme big data, we can't unpack the data at once and we need to
    // chunk it and unpack each chunk separately.
    // MAX 2GB is the limit right now to allocate a buffer
    return getUnpackedChunks(data, options.maxBytesPerChunk);
  }
  var pixelData = new Uint8Array(data);
  var max = multiframe.MaximumFractionalValue;
  var onlyMaxAndZero = pixelData.find(function (element) {
    return element !== 0 && element !== max;
  }) === undefined;
  if (!onlyMaxAndZero) {
    // This is a fractional segmentation, which is not currently supported.
    return;
  }
  dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .log */ .cM.warn("This segmentation object is actually binary... processing as such.");
  return pixelData;
}
function getUnpackedChunks(data, maxBytesPerChunk) {
  var bitArray = new Uint8Array(data);
  var chunks = [];
  var maxBitsPerChunk = maxBytesPerChunk * 8;
  var numberOfChunks = Math.ceil(bitArray.length * 8 / maxBitsPerChunk);
  for (var i = 0; i < numberOfChunks; i++) {
    var startBit = i * maxBitsPerChunk;
    var endBit = Math.min(startBit + maxBitsPerChunk, bitArray.length * 8);
    var startByte = Math.floor(startBit / 8);
    var endByte = Math.ceil(endBit / 8);
    var chunk = bitArray.slice(startByte, endByte);
    var unpackedChunk = BitArray$1.unpack(chunk);
    chunks.push(unpackedChunk);
  }
  return chunks;
}

/**
 * getImageIdOfSourceImageBySourceImageSequence - Returns the Cornerstone imageId of the source image.
 *
 * @param  {Object}   SourceImageSequence  Sequence describing the source image.
 * @param  {String[]} imageIds             A list of imageIds.
 * @param  {Object}   sopUIDImageIdIndexMap A map of SOPInstanceUIDs to imageIds.
 * @return {String}                        The corresponding imageId.
 */
function getImageIdOfSourceImageBySourceImageSequence(SourceImageSequence, sopUIDImageIdIndexMap) {
  var ReferencedSOPInstanceUID = SourceImageSequence.ReferencedSOPInstanceUID,
    ReferencedFrameNumber = SourceImageSequence.ReferencedFrameNumber;
  return ReferencedFrameNumber ? getImageIdOfReferencedFrame(ReferencedSOPInstanceUID, ReferencedFrameNumber, sopUIDImageIdIndexMap) : sopUIDImageIdIndexMap[ReferencedSOPInstanceUID];
}

/**
 * getImageIdOfSourceImagebyGeometry - Returns the Cornerstone imageId of the source image.
 *
 * @param  {String}    ReferencedSeriesInstanceUID    Referenced series of the source image.
 * @param  {String}    FrameOfReferenceUID            Frame of reference.
 * @param  {Object}    PerFrameFunctionalGroup        Sequence describing segmentation reference attributes per frame.
 * @param  {String[]}  imageIds                       A list of imageIds.
 * @param  {Object}    sopUIDImageIdIndexMap          A map of SOPInstanceUIDs to imageIds.
 * @param  {Float}     tolerance                      The tolerance parameter
 *
 * @return {String}                                   The corresponding imageId.
 */
function getImageIdOfSourceImagebyGeometry(ReferencedSeriesInstanceUID, FrameOfReferenceUID, PerFrameFunctionalGroup, imageIds, metadataProvider, tolerance) {
  if (ReferencedSeriesInstanceUID === undefined || PerFrameFunctionalGroup.PlanePositionSequence === undefined || PerFrameFunctionalGroup.PlanePositionSequence[0] === undefined || PerFrameFunctionalGroup.PlanePositionSequence[0].ImagePositionPatient === undefined) {
    return undefined;
  }
  for (var imageIdsIndexc = 0; imageIdsIndexc < imageIds.length; ++imageIdsIndexc) {
    var sourceImageMetadata = metadataProvider.get("instance", imageIds[imageIdsIndexc]);
    if (sourceImageMetadata === undefined || sourceImageMetadata.ImagePositionPatient === undefined || sourceImageMetadata.FrameOfReferenceUID !== FrameOfReferenceUID || sourceImageMetadata.SeriesInstanceUID !== ReferencedSeriesInstanceUID) {
      continue;
    }
    if (compareArrays(PerFrameFunctionalGroup.PlanePositionSequence[0].ImagePositionPatient, sourceImageMetadata.ImagePositionPatient, tolerance)) {
      return imageIds[imageIdsIndexc];
    }
  }
}

/**
 * getImageIdOfReferencedFrame - Returns the imageId corresponding to the
 * specified sopInstanceUid and frameNumber for multi-frame images.
 *
 * @param  {String} sopInstanceUid   The sopInstanceUid of the desired image.
 * @param  {Number} frameNumber      The frame number.
 * @param  {String} imageIds         The list of imageIds.
 * @param  {Object} sopUIDImageIdIndexMap A map of SOPInstanceUIDs to imageIds.
 * @return {String}                  The imageId that corresponds to the sopInstanceUid.
 */
function getImageIdOfReferencedFrame(sopInstanceUid, frameNumber, sopUIDImageIdIndexMap) {
  var imageId = sopUIDImageIdIndexMap[sopInstanceUid];
  if (!imageId) {
    return;
  }
  var imageIdFrameNumber = Number(imageId.split("frame=")[1]);
  return imageIdFrameNumber === frameNumber - 1 ? imageId : undefined;
}

/**
 * getValidOrientations - returns an array of valid orientations.
 *
 * @param  {Number[6]} iop The row (0..2) an column (3..5) direction cosines.
 * @return {Number[8][6]} An array of valid orientations.
 */
function getValidOrientations(iop) {
  var orientations = [];

  // [0,  1,  2]: 0,   0hf,   0vf
  // [3,  4,  5]: 90,  90hf,  90vf
  // [6, 7]:      180, 270

  orientations[0] = iop;
  orientations[1] = flipIOP.h(iop);
  orientations[2] = flipIOP.v(iop);
  var iop90 = rotateDirectionCosinesInPlane(iop, Math.PI / 2);
  orientations[3] = iop90;
  orientations[4] = flipIOP.h(iop90);
  orientations[5] = flipIOP.v(iop90);
  orientations[6] = rotateDirectionCosinesInPlane(iop, Math.PI);
  orientations[7] = rotateDirectionCosinesInPlane(iop, 1.5 * Math.PI);
  return orientations;
}

/**
 * alignPixelDataWithSourceData -
 *
 * @param {Ndarray} pixelData2D - The data to align.
 * @param {Number[6]} iop - The orientation of the image slice.
 * @param {Number[8][6]} orientations - An array of valid imageOrientationPatient values.
 * @param {Number} tolerance.
 * @return {Ndarray} The aligned pixelData.
 */
function alignPixelDataWithSourceData(pixelData2D, iop, orientations, tolerance) {
  if (compareArrays(iop, orientations[0], tolerance)) {
    return pixelData2D;
  } else if (compareArrays(iop, orientations[1], tolerance)) {
    // Flipped vertically.

    // Undo Flip
    return flipMatrix2D.v(pixelData2D);
  } else if (compareArrays(iop, orientations[2], tolerance)) {
    // Flipped horizontally.

    // Unfo flip
    return flipMatrix2D.h(pixelData2D);
  } else if (compareArrays(iop, orientations[3], tolerance)) {
    //Rotated 90 degrees

    // Rotate back
    return rotateMatrix902D(pixelData2D);
  } else if (compareArrays(iop, orientations[4], tolerance)) {
    //Rotated 90 degrees and fliped horizontally.

    // Undo flip and rotate back.
    return rotateMatrix902D(flipMatrix2D.h(pixelData2D));
  } else if (compareArrays(iop, orientations[5], tolerance)) {
    // Rotated 90 degrees and fliped vertically

    // Unfo flip and rotate back.
    return rotateMatrix902D(flipMatrix2D.v(pixelData2D));
  } else if (compareArrays(iop, orientations[6], tolerance)) {
    // Rotated 180 degrees. // TODO -> Do this more effeciently, there is a 1:1 mapping like 90 degree rotation.

    return rotateMatrix902D(rotateMatrix902D(pixelData2D));
  } else if (compareArrays(iop, orientations[7], tolerance)) {
    // Rotated 270 degrees

    // Rotate back.
    return rotateMatrix902D(rotateMatrix902D(rotateMatrix902D(pixelData2D)));
  }
}

/**
 * compareArrays - Returns true if array1 and array2 are equal
 * within a tolerance.
 *
 * @param  {Number[]} array1 - An array.
 * @param  {Number[]} array2 - An array.
 * @param {Number} tolerance.
 * @return {Boolean} True if array1 and array2 are equal.
 */
function compareArrays(array1, array2, tolerance) {
  if (array1.length != array2.length) {
    return false;
  }
  for (var i = 0; i < array1.length; ++i) {
    if (!nearlyEqual(array1[i], array2[i], tolerance)) {
      return false;
    }
  }
  return true;
}
function getSegmentMetadata(multiframe, seriesInstanceUid) {
  var segmentSequence = multiframe.SegmentSequence;
  var data = [];
  if (Array.isArray(segmentSequence)) {
    data = [undefined].concat(_toConsumableArray(segmentSequence));
  } else {
    // Only one segment, will be stored as an object.
    data = [undefined, segmentSequence];
  }
  return {
    seriesInstanceUid: seriesInstanceUid,
    data: data
  };
}

/**
 * Reads a range of bytes from an array of ArrayBuffer chunks and
 * aggregate them into a new Uint8Array.
 *
 * @param {ArrayBuffer[]} chunks - An array of ArrayBuffer chunks.
 * @param {number} offset - The offset of the first byte to read.
 * @param {number} length - The number of bytes to read.
 * @returns {Uint8Array} A new Uint8Array containing the requested bytes.
 */
function readFromUnpackedChunks(chunks, offset, length) {
  var mapping = getUnpackedOffsetAndLength(chunks, offset, length);

  // If all the data is in one chunk, we can just slice that chunk
  if (mapping.start.chunkIndex === mapping.end.chunkIndex) {
    return new Uint8Array(chunks[mapping.start.chunkIndex].buffer, mapping.start.offset, length);
  } else {
    // If the data spans multiple chunks, we need to create a new Uint8Array and copy the data from each chunk
    var result = new Uint8Array(length);
    var resultOffset = 0;
    for (var i = mapping.start.chunkIndex; i <= mapping.end.chunkIndex; i++) {
      var start = i === mapping.start.chunkIndex ? mapping.start.offset : 0;
      var end = i === mapping.end.chunkIndex ? mapping.end.offset : chunks[i].length;
      result.set(new Uint8Array(chunks[i].buffer, start, end - start), resultOffset);
      resultOffset += end - start;
    }
    return result;
  }
}
function getUnpackedOffsetAndLength(chunks, offset, length) {
  var totalBytes = chunks.reduce(function (total, chunk) {
    return total + chunk.length;
  }, 0);
  if (offset < 0 || offset + length > totalBytes) {
    throw new Error("Offset and length out of bounds");
  }
  var startChunkIndex = 0;
  var startOffsetInChunk = offset;
  while (startOffsetInChunk >= chunks[startChunkIndex].length) {
    startOffsetInChunk -= chunks[startChunkIndex].length;
    startChunkIndex++;
  }
  var endChunkIndex = startChunkIndex;
  var endOffsetInChunk = startOffsetInChunk + length;
  while (endOffsetInChunk > chunks[endChunkIndex].length) {
    endOffsetInChunk -= chunks[endChunkIndex].length;
    endChunkIndex++;
  }
  return {
    start: {
      chunkIndex: startChunkIndex,
      offset: startOffsetInChunk
    },
    end: {
      chunkIndex: endChunkIndex,
      offset: endOffsetInChunk
    }
  };
}
function calculateCentroid(imageIdIndexBufferIndex, multiframe) {
  var xAcc = 0;
  var yAcc = 0;
  var zAcc = 0;
  var count = 0;
  for (var _i3 = 0, _Object$entries = Object.entries(imageIdIndexBufferIndex); _i3 < _Object$entries.length; _i3++) {
    var _Object$entries$_i = _slicedToArray(_Object$entries[_i3], 2),
      imageIdIndex = _Object$entries$_i[0],
      bufferIndices = _Object$entries$_i[1];
    var z = Number(imageIdIndex);
    if (!bufferIndices || bufferIndices.length === 0) {
      continue;
    }
    var _iterator2 = _createForOfIteratorHelper(bufferIndices),
      _step2;
    try {
      for (_iterator2.s(); !(_step2 = _iterator2.n()).done;) {
        var bufferIndex = _step2.value;
        var y = Math.floor(bufferIndex / multiframe.Rows);
        var x = bufferIndex % multiframe.Rows;
        xAcc += x;
        yAcc += y;
        zAcc += z;
        count++;
      }
    } catch (err) {
      _iterator2.e(err);
    } finally {
      _iterator2.f();
    }
  }
  return {
    xAcc: xAcc,
    yAcc: yAcc,
    zAcc: zAcc,
    count: count
  };
}
var Segmentation$4 = {
  generateSegmentation: generateSegmentation$2,
  generateToolState: generateToolState$2,
  fillSegmentation: fillSegmentation$1
};

var Segmentation$3 = {
  generateSegmentation: generateSegmentation$1,
  generateToolState: generateToolState$1,
  fillSegmentation: fillSegmentation
};

/**
 * generateSegmentation - Generates a DICOM Segmentation object given cornerstoneTools data.
 *
 * @param  {object[]} images    An array of the cornerstone image objects.
 * @param  {Object|Object[]} labelmaps3DorBrushData For 4.X: The cornerstone `Labelmap3D` object, or an array of objects.
 *                                                  For 3.X: the BrushData.
 * @param  {number} cornerstoneToolsVersion The cornerstoneTools major version to map against.
 * @returns {Object}
 */
function generateSegmentation$1(images, labelmaps3DorBrushData) {
  var options = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {
    includeSliceSpacing: true
  };
  var cornerstoneToolsVersion = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : 4;
  if (cornerstoneToolsVersion === 4) {
    return Segmentation$4.generateSegmentation(images, labelmaps3DorBrushData, options);
  }
  if (cornerstoneToolsVersion === 3) {
    return Segmentation$5.generateSegmentation(images, labelmaps3DorBrushData, options);
  }
  console.warn("No generateSegmentation adapater for cornerstone version ".concat(cornerstoneToolsVersion, ", exiting."));
}

/**
 * generateToolState - Given a set of cornrstoneTools imageIds and a Segmentation buffer,
 * derive cornerstoneTools toolState and brush metadata.
 *
 * @param  {string[]} imageIds    An array of the imageIds.
 * @param  {ArrayBuffer} arrayBuffer The SEG arrayBuffer.
 * @param {*} metadataProvider
 * @param  {bool} skipOverlapping - skip checks for overlapping segs, default value false.
 * @param  {number} tolerance - default value 1.e-3.
 * @param  {number} cornerstoneToolsVersion - default value 4.
 *
 * @returns {Object}  The toolState and an object from which the
 *                    segment metadata can be derived.
 */
function generateToolState$1(imageIds, arrayBuffer, metadataProvider) {
  var skipOverlapping = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : false;
  var tolerance = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : 1e-3;
  var cornerstoneToolsVersion = arguments.length > 5 && arguments[5] !== undefined ? arguments[5] : 4;
  if (cornerstoneToolsVersion === 4) {
    return Segmentation$4.generateToolState(imageIds, arrayBuffer, metadataProvider, skipOverlapping, tolerance);
  }
  if (cornerstoneToolsVersion === 3) {
    return Segmentation$5.generateToolState(imageIds, arrayBuffer, metadataProvider);
  }
  console.warn("No generateToolState adapater for cornerstone version ".concat(cornerstoneToolsVersion, ", exiting."));
}

/**
 * fillSegmentation - Fills a derived segmentation dataset with cornerstoneTools `LabelMap3D` data.
 *
 * @param  {object[]} segmentation An empty segmentation derived dataset.
 * @param  {Object|Object[]} inputLabelmaps3D The cornerstone `Labelmap3D` object, or an array of objects.
 * @param  {Object} userOptions Options object to override default options.
 * @returns {Blob}           description
 */
function fillSegmentation(segmentation, inputLabelmaps3D) {
  var options = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : {
    includeSliceSpacing: true
  };
  var cornerstoneToolsVersion = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : 4;
  if (cornerstoneToolsVersion === 4) {
    return Segmentation$4.fillSegmentation(segmentation, inputLabelmaps3D, options);
  }
  console.warn("No generateSegmentation adapater for cornerstone version ".concat(cornerstoneToolsVersion, ", exiting."));
}

var CornerstoneSR = {
    Length: Length$1,
    FreehandRoi: FreehandRoi,
    Bidirectional: Bidirectional$1,
    EllipticalRoi: EllipticalRoi,
    CircleRoi: CircleRoi,
    ArrowAnnotate: ArrowAnnotate$1,
    MeasurementReport: MeasurementReport$1,
    CobbAngle: CobbAngle$1,
    Angle: Angle$1,
    RectangleRoi: RectangleRoi
};
var CornerstoneSEG = {
    Segmentation: Segmentation$3
};

/******************************************************************************
Copyright (c) Microsoft Corporation.

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.
***************************************************************************** */
var __assign = function () {
  __assign = Object.assign || function __assign(t) {
    for (var s, i = 1, n = arguments.length; i < n; i++) {
      s = arguments[i];
      for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p)) t[p] = s[p];
    }
    return t;
  };
  return __assign.apply(this, arguments);
};
function __spreadArray(to, from, pack) {
  if (pack || arguments.length === 2) for (var i = 0, l = from.length, ar; i < l; i++) {
    if (ar || !(i in from)) {
      if (!ar) ar = Array.prototype.slice.call(from, 0, i);
      ar[i] = from[i];
    }
  }
  return to.concat(ar || Array.prototype.slice.call(from));
}

var CORNERSTONE_3D_TAG = "Cornerstone3DTools@^0.1.0";

// This is a custom coding scheme defined to store some annotations from Cornerstone.
// Note: CodeMeaning is VR type LO, which means we only actually support 64 characters
// here this is fine for most labels, but may be problematic at some point.
var CORNERSTONEFREETEXT = "CORNERSTONEFREETEXT";

// Cornerstone specified coding scheme for storing findings
var CodingSchemeDesignator$1 = "CORNERSTONEJS";
var CodingScheme = {
  CodingSchemeDesignator: CodingSchemeDesignator$1,
  codeValues: {
    CORNERSTONEFREETEXT: CORNERSTONEFREETEXT
  }
};

var TID1500 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID1500, addAccessors = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.addAccessors;
var StructuredReport = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .derivations */ .U7.StructuredReport;
var Normalizer$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .normalizers */ .oq.Normalizer;
var TID1500MeasurementReport = TID1500.TID1500MeasurementReport, TID1501MeasurementGroup = TID1500.TID1501MeasurementGroup;
var DicomMetaDictionary$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .data */ .aT.DicomMetaDictionary;
var FINDING = { CodingSchemeDesignator: "DCM", CodeValue: "121071" };
var FINDING_SITE = { CodingSchemeDesignator: "SCT", CodeValue: "363698007" };
var FINDING_SITE_OLD = { CodingSchemeDesignator: "SRT", CodeValue: "G-C0E3" };
var codeValueMatch = function (group, code, oldCode) {
    var ConceptNameCodeSequence = group.ConceptNameCodeSequence;
    if (!ConceptNameCodeSequence)
        return;
    var CodingSchemeDesignator = ConceptNameCodeSequence.CodingSchemeDesignator, CodeValue = ConceptNameCodeSequence.CodeValue;
    return ((CodingSchemeDesignator == code.CodingSchemeDesignator &&
        CodeValue == code.CodeValue) ||
        (oldCode &&
            CodingSchemeDesignator == oldCode.CodingSchemeDesignator &&
            CodeValue == oldCode.CodeValue));
};
function getTID300ContentItem(tool, toolType, ReferencedSOPSequence, toolClass, worldToImageCoords) {
    var args = toolClass.getTID300RepresentationArguments(tool, worldToImageCoords);
    args.ReferencedSOPSequence = ReferencedSOPSequence;
    var TID300Measurement = new toolClass.TID300Representation(args);
    return TID300Measurement;
}
function getMeasurementGroup(toolType, toolData, ReferencedSOPSequence, worldToImageCoords) {
    var toolTypeData = toolData[toolType];
    var toolClass = MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_TOOL_TYPE[toolType];
    if (!toolTypeData ||
        !toolTypeData.data ||
        !toolTypeData.data.length ||
        !toolClass) {
        return;
    }
    // Loop through the array of tool instances
    // for this tool
    var Measurements = toolTypeData.data.map(function (tool) {
        return getTID300ContentItem(tool, toolType, ReferencedSOPSequence, toolClass, worldToImageCoords);
    });
    return new TID1501MeasurementGroup(Measurements);
}
var MeasurementReport = /** @class */ (function () {
    function MeasurementReport() {
    }
    MeasurementReport.getCornerstoneLabelFromDefaultState = function (defaultState) {
        var _a = defaultState.findingSites, findingSites = _a === void 0 ? [] : _a, finding = defaultState.finding;
        var cornersoneFreeTextCodingValue = CodingScheme.codeValues.CORNERSTONEFREETEXT;
        var freeTextLabel = findingSites.find(function (fs) { return fs.CodeValue === cornersoneFreeTextCodingValue; });
        if (freeTextLabel) {
            return freeTextLabel.CodeMeaning;
        }
        if (finding && finding.CodeValue === cornersoneFreeTextCodingValue) {
            return finding.CodeMeaning;
        }
    };
    MeasurementReport.generateDatasetMeta = function () {
        // TODO: what is the correct metaheader
        // http://dicom.nema.org/medical/Dicom/current/output/chtml/part10/chapter_7.html
        // TODO: move meta creation to happen in derivations.js
        var fileMetaInformationVersionArray = new Uint8Array(2);
        fileMetaInformationVersionArray[1] = 1;
        var _meta = {
            FileMetaInformationVersion: {
                Value: [fileMetaInformationVersionArray.buffer],
                vr: "OB"
            },
            //MediaStorageSOPClassUID
            //MediaStorageSOPInstanceUID: sopCommonModule.sopInstanceUID,
            TransferSyntaxUID: {
                Value: ["1.2.840.10008.1.2.1"],
                vr: "UI"
            },
            ImplementationClassUID: {
                Value: [DicomMetaDictionary$1.uid()],
                vr: "UI"
            },
            ImplementationVersionName: {
                Value: ["dcmjs"],
                vr: "SH"
            }
        };
        return _meta;
    };
    MeasurementReport.getSetupMeasurementData = function (MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, toolType) {
        var ContentSequence = MeasurementGroup.ContentSequence;
        var contentSequenceArr = toArray(ContentSequence);
        var findingGroup = contentSequenceArr.find(function (group) {
            return codeValueMatch(group, FINDING);
        });
        var findingSiteGroups = contentSequenceArr.filter(function (group) {
            return codeValueMatch(group, FINDING_SITE, FINDING_SITE_OLD);
        }) || [];
        var NUMGroup = contentSequenceArr.find(function (group) { return group.ValueType === "NUM"; });
        var SCOORDGroup = toArray(NUMGroup.ContentSequence).find(function (group) { return group.ValueType === "SCOORD"; });
        var ReferencedSOPSequence = SCOORDGroup.ContentSequence.ReferencedSOPSequence;
        var ReferencedSOPInstanceUID = ReferencedSOPSequence.ReferencedSOPInstanceUID, ReferencedFrameNumber = ReferencedSOPSequence.ReferencedFrameNumber;
        var referencedImageId = sopInstanceUIDToImageIdMap[ReferencedSOPInstanceUID];
        var imagePlaneModule = metadata.get("imagePlaneModule", referencedImageId);
        var finding = findingGroup
            ? addAccessors(findingGroup.ConceptCodeSequence)
            : undefined;
        var findingSites = findingSiteGroups.map(function (fsg) {
            return addAccessors(fsg.ConceptCodeSequence);
        });
        var defaultState = {
            description: undefined,
            sopInstanceUid: ReferencedSOPInstanceUID,
            annotation: {
                annotationUID: DicomMetaDictionary$1.uid(),
                metadata: {
                    toolName: toolType,
                    referencedImageId: referencedImageId,
                    FrameOfReferenceUID: imagePlaneModule.frameOfReferenceUID,
                    label: ""
                },
                data: undefined
            },
            finding: finding,
            findingSites: findingSites
        };
        if (defaultState.finding) {
            defaultState.description = defaultState.finding.CodeMeaning;
        }
        defaultState.annotation.metadata.label =
            MeasurementReport.getCornerstoneLabelFromDefaultState(defaultState);
        return {
            defaultState: defaultState,
            NUMGroup: NUMGroup,
            SCOORDGroup: SCOORDGroup,
            ReferencedSOPSequence: ReferencedSOPSequence,
            ReferencedSOPInstanceUID: ReferencedSOPInstanceUID,
            ReferencedFrameNumber: ReferencedFrameNumber
        };
    };
    MeasurementReport.generateReport = function (toolState, metadataProvider, worldToImageCoords, options) {
        // ToolState for array of imageIDs to a Report
        // Assume Cornerstone metadata provider has access to Study / Series / Sop Instance UID
        var allMeasurementGroups = [];
        /* Patient ID
        Warning - Missing attribute or value that would be needed to build DICOMDIR - Patient ID
        Warning - Missing attribute or value that would be needed to build DICOMDIR - Study Date
        Warning - Missing attribute or value that would be needed to build DICOMDIR - Study Time
        Warning - Missing attribute or value that would be needed to build DICOMDIR - Study ID
        */
        var sopInstanceUIDsToSeriesInstanceUIDMap = {};
        var derivationSourceDatasets = [];
        var _meta = MeasurementReport.generateDatasetMeta();
        // Loop through each image in the toolData
        Object.keys(toolState).forEach(function (imageId) {
            var sopCommonModule = metadataProvider.get("sopCommonModule", imageId);
            var instance = metadataProvider.get("instance", imageId);
            var sopInstanceUID = sopCommonModule.sopInstanceUID, sopClassUID = sopCommonModule.sopClassUID;
            var seriesInstanceUID = instance.SeriesInstanceUID;
            sopInstanceUIDsToSeriesInstanceUIDMap[sopInstanceUID] =
                seriesInstanceUID;
            if (!derivationSourceDatasets.find(function (dsd) { return dsd.SeriesInstanceUID === seriesInstanceUID; })) {
                // Entry not present for series, create one.
                var derivationSourceDataset = MeasurementReport.generateDerivationSourceDataset(instance);
                derivationSourceDatasets.push(derivationSourceDataset);
            }
            var frameNumber = metadataProvider.get("frameNumber", imageId);
            var toolData = toolState[imageId];
            var toolTypes = Object.keys(toolData);
            var ReferencedSOPSequence = {
                ReferencedSOPClassUID: sopClassUID,
                ReferencedSOPInstanceUID: sopInstanceUID,
                ReferencedFrameNumber: undefined
            };
            if ((instance &&
                instance.NumberOfFrames &&
                instance.NumberOfFrames > 1) ||
                Normalizer$1.isMultiframeSOPClassUID(sopClassUID)) {
                ReferencedSOPSequence.ReferencedFrameNumber = frameNumber;
            }
            // Loop through each tool type for the image
            var measurementGroups = [];
            toolTypes.forEach(function (toolType) {
                var group = getMeasurementGroup(toolType, toolData, ReferencedSOPSequence, worldToImageCoords);
                if (group) {
                    measurementGroups.push(group);
                }
            });
            allMeasurementGroups =
                allMeasurementGroups.concat(measurementGroups);
        });
        var tid1500MeasurementReport = new TID1500MeasurementReport({ TID1501MeasurementGroups: allMeasurementGroups }, options);
        var report = new StructuredReport(derivationSourceDatasets, options);
        var contentItem = tid1500MeasurementReport.contentItem(derivationSourceDatasets, __assign(__assign({}, options), { sopInstanceUIDsToSeriesInstanceUIDMap: sopInstanceUIDsToSeriesInstanceUIDMap }));
        // Merge the derived dataset with the content from the Measurement Report
        report.dataset = Object.assign(report.dataset, contentItem);
        report.dataset._meta = _meta;
        return report;
    };
    /**
     * Generate Cornerstone tool state from dataset
     */
    MeasurementReport.generateToolState = function (dataset, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata, hooks) {
        // For now, bail out if the dataset is not a TID1500 SR with length measurements
        if (dataset.ContentTemplateSequence.TemplateIdentifier !== "1500") {
            throw new Error("This package can currently only interpret DICOM SR TID 1500");
        }
        var REPORT = "Imaging Measurements";
        var GROUP = "Measurement Group";
        var TRACKING_IDENTIFIER = "Tracking Identifier";
        // Identify the Imaging Measurements
        var imagingMeasurementContent = toArray(dataset.ContentSequence).find(codeMeaningEquals(REPORT));
        // Retrieve the Measurements themselves
        var measurementGroups = toArray(imagingMeasurementContent.ContentSequence).filter(codeMeaningEquals(GROUP));
        // For each of the supported measurement types, compute the measurement data
        var measurementData = {};
        var cornerstoneToolClasses = MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_UTILITY_TYPE;
        var registeredToolClasses = [];
        Object.keys(cornerstoneToolClasses).forEach(function (key) {
            registeredToolClasses.push(cornerstoneToolClasses[key]);
            measurementData[key] = [];
        });
        measurementGroups.forEach(function (measurementGroup) {
            var _a;
            try {
                var measurementGroupContentSequence = toArray(measurementGroup.ContentSequence);
                var TrackingIdentifierGroup = measurementGroupContentSequence.find(function (contentItem) {
                    return contentItem.ConceptNameCodeSequence.CodeMeaning ===
                        TRACKING_IDENTIFIER;
                });
                var TrackingIdentifierValue_1 = TrackingIdentifierGroup.TextValue;
                var toolClass = ((_a = hooks === null || hooks === void 0 ? void 0 : hooks.getToolClass) === null || _a === void 0 ? void 0 : _a.call(hooks, measurementGroup, dataset, registeredToolClasses)) ||
                    registeredToolClasses.find(function (tc) {
                        return tc.isValidCornerstoneTrackingIdentifier(TrackingIdentifierValue_1);
                    });
                if (toolClass) {
                    var measurement = toolClass.getMeasurementData(measurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata);
                    console.log("=== ".concat(toolClass.toolType, " ==="));
                    console.log(measurement);
                    measurementData[toolClass.toolType].push(measurement);
                }
            }
            catch (e) {
                console.warn("Unable to generate tool state for", measurementGroup, e);
            }
        });
        // NOTE: There is no way of knowing the cornerstone imageIds as that could be anything.
        // That is up to the consumer to derive from the SOPInstanceUIDs.
        return measurementData;
    };
    /**
     * Register a new tool type.
     * @param toolClass to perform I/O to DICOM for this tool
     */
    MeasurementReport.registerTool = function (toolClass) {
        MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_UTILITY_TYPE[toolClass.utilityToolType] = toolClass;
        MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_TOOL_TYPE[toolClass.toolType] = toolClass;
        MeasurementReport.MEASUREMENT_BY_TOOLTYPE[toolClass.toolType] =
            toolClass.utilityToolType;
    };
    MeasurementReport.CORNERSTONE_3D_TAG = CORNERSTONE_3D_TAG;
    MeasurementReport.MEASUREMENT_BY_TOOLTYPE = {};
    MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_UTILITY_TYPE = {};
    MeasurementReport.CORNERSTONE_TOOL_CLASSES_BY_TOOL_TYPE = {};
    MeasurementReport.generateDerivationSourceDataset = function (instance) {
        var _vrMap = {
            PixelData: "OW"
        };
        var _meta = MeasurementReport.generateDatasetMeta();
        var derivationSourceDataset = __assign(__assign({}, instance), { _meta: _meta, _vrMap: _vrMap });
        return derivationSourceDataset;
    };
    return MeasurementReport;
}());

var TID300Point$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Point;
var ARROW_ANNOTATE = "ArrowAnnotate";
var trackingIdentifierTextValue$7 = "".concat(CORNERSTONE_3D_TAG, ":").concat(ARROW_ANNOTATE);
var codeValues = CodingScheme.codeValues,
  CodingSchemeDesignator = CodingScheme.CodingSchemeDesignator;
var ArrowAnnotate = /*#__PURE__*/function () {
  function ArrowAnnotate() {
    _classCallCheck(this, ArrowAnnotate);
  }
  _createClass(ArrowAnnotate, null, [{
    key: "getMeasurementData",
    value: function getMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
      var _MeasurementReport$ge = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, ArrowAnnotate.toolType),
        defaultState = _MeasurementReport$ge.defaultState,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup,
        ReferencedFrameNumber = _MeasurementReport$ge.ReferencedFrameNumber;
      var referencedImageId = defaultState.annotation.metadata.referencedImageId;
      var text = defaultState.annotation.metadata.label;
      var GraphicData = SCOORDGroup.GraphicData;
      var worldCoords = [];
      for (var i = 0; i < GraphicData.length; i += 2) {
        var point = imageToWorldCoords(referencedImageId, [GraphicData[i], GraphicData[i + 1]]);
        worldCoords.push(point);
      }

      // Since the arrowAnnotate measurement is just a point, to generate the tool state
      // we derive the second point based on the image size relative to the first point.
      if (worldCoords.length === 1) {
        var imagePixelModule = metadata.get("imagePixelModule", referencedImageId);
        var xOffset = 10;
        var yOffset = 10;
        if (imagePixelModule) {
          var columns = imagePixelModule.columns,
            rows = imagePixelModule.rows;
          xOffset = columns / 10;
          yOffset = rows / 10;
        }
        var secondPoint = imageToWorldCoords(referencedImageId, [GraphicData[0] + xOffset, GraphicData[1] + yOffset]);
        worldCoords.push(secondPoint);
      }
      var state = defaultState;
      state.annotation.data = {
        text: text,
        handles: {
          arrowFirst: true,
          points: [worldCoords[0], worldCoords[1]],
          activeHandleIndex: 0,
          textBox: {
            hasMoved: false
          }
        },
        frameNumber: ReferencedFrameNumber
      };
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool, worldToImageCoords) {
      var data = tool.data,
        metadata = tool.metadata;
      var finding = tool.finding,
        findingSites = tool.findingSites;
      var referencedImageId = metadata.referencedImageId;
      if (!referencedImageId) {
        throw new Error("ArrowAnnotate.getTID300RepresentationArguments: referencedImageId is not defined");
      }
      var _data$handles = data.handles,
        points = _data$handles.points,
        arrowFirst = _data$handles.arrowFirst;
      var point;
      if (arrowFirst) {
        point = points[0];
      } else {
        point = points[1];
      }
      var pointImage = worldToImageCoords(referencedImageId, point);
      var TID300RepresentationArguments = {
        points: [{
          x: pointImage[0],
          y: pointImage[1]
        }],
        trackingIdentifierTextValue: trackingIdentifierTextValue$7,
        findingSites: findingSites || []
      };

      // If freetext finding isn't present, add it from the tool text.
      if (!finding || finding.CodeValue !== codeValues.CORNERSTONEFREETEXT) {
        finding = {
          CodeValue: codeValues.CORNERSTONEFREETEXT,
          CodingSchemeDesignator: CodingSchemeDesignator,
          CodeMeaning: data.text
        };
      }
      TID300RepresentationArguments.finding = finding;
      return TID300RepresentationArguments;
    }
  }]);
  return ArrowAnnotate;
}();
ArrowAnnotate.toolType = ARROW_ANNOTATE;
ArrowAnnotate.utilityToolType = ARROW_ANNOTATE;
ArrowAnnotate.TID300Representation = TID300Point$1;
ArrowAnnotate.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone3DTag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
    return false;
  }
  return toolType === ARROW_ANNOTATE;
};
MeasurementReport.registerTool(ArrowAnnotate);

var TID300Bidirectional = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Bidirectional;
var BIDIRECTIONAL = "Bidirectional";
var LONG_AXIS = "Long Axis";
var SHORT_AXIS = "Short Axis";
var trackingIdentifierTextValue$6 = "".concat(CORNERSTONE_3D_TAG, ":").concat(BIDIRECTIONAL);
var Bidirectional = /** @class */ (function () {
    function Bidirectional() {
    }
    Bidirectional.getMeasurementData = function (MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
        var _a;
        var _b = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, Bidirectional.toolType), defaultState = _b.defaultState, ReferencedFrameNumber = _b.ReferencedFrameNumber;
        var referencedImageId = defaultState.annotation.metadata.referencedImageId;
        var ContentSequence = MeasurementGroup.ContentSequence;
        var longAxisNUMGroup = toArray(ContentSequence).find(function (group) { return group.ConceptNameCodeSequence.CodeMeaning === LONG_AXIS; });
        var longAxisSCOORDGroup = toArray(longAxisNUMGroup.ContentSequence).find(function (group) { return group.ValueType === "SCOORD"; });
        var shortAxisNUMGroup = toArray(ContentSequence).find(function (group) { return group.ConceptNameCodeSequence.CodeMeaning === SHORT_AXIS; });
        var shortAxisSCOORDGroup = toArray(shortAxisNUMGroup.ContentSequence).find(function (group) { return group.ValueType === "SCOORD"; });
        var worldCoords = [];
        [longAxisSCOORDGroup, shortAxisSCOORDGroup].forEach(function (group) {
            var GraphicData = group.GraphicData;
            for (var i = 0; i < GraphicData.length; i += 2) {
                var point = imageToWorldCoords(referencedImageId, [
                    GraphicData[i],
                    GraphicData[i + 1]
                ]);
                worldCoords.push(point);
            }
        });
        var state = defaultState;
        state.annotation.data = {
            handles: {
                points: [
                    worldCoords[0],
                    worldCoords[1],
                    worldCoords[2],
                    worldCoords[3]
                ],
                activeHandleIndex: 0,
                textBox: {
                    hasMoved: false
                }
            },
            cachedStats: (_a = {},
                _a["imageId:".concat(referencedImageId)] = {
                    length: longAxisNUMGroup.MeasuredValueSequence.NumericValue,
                    width: shortAxisNUMGroup.MeasuredValueSequence.NumericValue
                },
                _a),
            frameNumber: ReferencedFrameNumber
        };
        return state;
    };
    Bidirectional.getTID300RepresentationArguments = function (tool, worldToImageCoords) {
        var data = tool.data, finding = tool.finding, findingSites = tool.findingSites, metadata = tool.metadata;
        var _a = data.cachedStats, cachedStats = _a === void 0 ? {} : _a, handles = data.handles;
        var referencedImageId = metadata.referencedImageId;
        if (!referencedImageId) {
            throw new Error("Bidirectional.getTID300RepresentationArguments: referencedImageId is not defined");
        }
        var _b = cachedStats["imageId:".concat(referencedImageId)] || {}, length = _b.length, width = _b.width;
        var points = handles.points;
        // Find the length and width point pairs by comparing the distances of the points at 0,1 to points at 2,3
        var firstPointPairs = [points[0], points[1]];
        var secondPointPairs = [points[2], points[3]];
        var firstPointPairsDistance = Math.sqrt(Math.pow(firstPointPairs[0][0] - firstPointPairs[1][0], 2) +
            Math.pow(firstPointPairs[0][1] - firstPointPairs[1][1], 2) +
            Math.pow(firstPointPairs[0][2] - firstPointPairs[1][2], 2));
        var secondPointPairsDistance = Math.sqrt(Math.pow(secondPointPairs[0][0] - secondPointPairs[1][0], 2) +
            Math.pow(secondPointPairs[0][1] - secondPointPairs[1][1], 2) +
            Math.pow(secondPointPairs[0][2] - secondPointPairs[1][2], 2));
        var shortAxisPoints;
        var longAxisPoints;
        if (firstPointPairsDistance > secondPointPairsDistance) {
            shortAxisPoints = firstPointPairs;
            longAxisPoints = secondPointPairs;
        }
        else {
            shortAxisPoints = secondPointPairs;
            longAxisPoints = firstPointPairs;
        }
        var longAxisStartImage = worldToImageCoords(referencedImageId, shortAxisPoints[0]);
        var longAxisEndImage = worldToImageCoords(referencedImageId, shortAxisPoints[1]);
        var shortAxisStartImage = worldToImageCoords(referencedImageId, longAxisPoints[0]);
        var shortAxisEndImage = worldToImageCoords(referencedImageId, longAxisPoints[1]);
        return {
            longAxis: {
                point1: {
                    x: longAxisStartImage[0],
                    y: longAxisStartImage[1]
                },
                point2: {
                    x: longAxisEndImage[0],
                    y: longAxisEndImage[1]
                }
            },
            shortAxis: {
                point1: {
                    x: shortAxisStartImage[0],
                    y: shortAxisStartImage[1]
                },
                point2: {
                    x: shortAxisEndImage[0],
                    y: shortAxisEndImage[1]
                }
            },
            longAxisLength: length,
            shortAxisLength: width,
            trackingIdentifierTextValue: trackingIdentifierTextValue$6,
            finding: finding,
            findingSites: findingSites || []
        };
    };
    Bidirectional.toolType = BIDIRECTIONAL;
    Bidirectional.utilityToolType = BIDIRECTIONAL;
    Bidirectional.TID300Representation = TID300Bidirectional;
    Bidirectional.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
        if (!TrackingIdentifier.includes(":")) {
            return false;
        }
        var _a = TrackingIdentifier.split(":"), cornerstone3DTag = _a[0], toolType = _a[1];
        if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
            return false;
        }
        return toolType === BIDIRECTIONAL;
    };
    return Bidirectional;
}());
MeasurementReport.registerTool(Bidirectional);

var TID300CobbAngle$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.CobbAngle;
var MEASUREMENT_TYPE$1 = "Angle";
var trackingIdentifierTextValue$5 = "".concat(CORNERSTONE_3D_TAG, ":").concat(MEASUREMENT_TYPE$1);
var Angle = /** @class */ (function () {
    function Angle() {
    }
    // TODO: this function is required for all Cornerstone Tool Adapters, since it is called by MeasurementReport.
    Angle.getMeasurementData = function (MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
        var _a;
        var _b = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, Angle.toolType), defaultState = _b.defaultState, NUMGroup = _b.NUMGroup, SCOORDGroup = _b.SCOORDGroup, ReferencedFrameNumber = _b.ReferencedFrameNumber;
        var referencedImageId = defaultState.annotation.metadata.referencedImageId;
        var GraphicData = SCOORDGroup.GraphicData;
        var worldCoords = [];
        for (var i = 0; i < GraphicData.length; i += 2) {
            var point = imageToWorldCoords(referencedImageId, [
                GraphicData[i],
                GraphicData[i + 1]
            ]);
            worldCoords.push(point);
        }
        var state = defaultState;
        state.annotation.data = {
            handles: {
                points: [worldCoords[0], worldCoords[1], worldCoords[3]],
                activeHandleIndex: 0,
                textBox: {
                    hasMoved: false
                }
            },
            cachedStats: (_a = {},
                _a["imageId:".concat(referencedImageId)] = {
                    angle: NUMGroup
                        ? NUMGroup.MeasuredValueSequence.NumericValue
                        : null
                },
                _a),
            frameNumber: ReferencedFrameNumber
        };
        return state;
    };
    Angle.getTID300RepresentationArguments = function (tool, worldToImageCoords) {
        var data = tool.data, finding = tool.finding, findingSites = tool.findingSites, metadata = tool.metadata;
        var _a = data.cachedStats, cachedStats = _a === void 0 ? {} : _a, handles = data.handles;
        var referencedImageId = metadata.referencedImageId;
        if (!referencedImageId) {
            throw new Error("Angle.getTID300RepresentationArguments: referencedImageId is not defined");
        }
        var start1 = worldToImageCoords(referencedImageId, handles.points[0]);
        var middle = worldToImageCoords(referencedImageId, handles.points[1]);
        var end = worldToImageCoords(referencedImageId, handles.points[2]);
        var point1 = { x: start1[0], y: start1[1] };
        var point2 = { x: middle[0], y: middle[1] };
        var point3 = point2;
        var point4 = { x: end[0], y: end[1] };
        var angle = (cachedStats["imageId:".concat(referencedImageId)] || {}).angle;
        // Represented as a cobb angle
        return {
            point1: point1,
            point2: point2,
            point3: point3,
            point4: point4,
            rAngle: angle,
            trackingIdentifierTextValue: trackingIdentifierTextValue$5,
            finding: finding,
            findingSites: findingSites || []
        };
    };
    Angle.toolType = MEASUREMENT_TYPE$1;
    Angle.utilityToolType = MEASUREMENT_TYPE$1;
    Angle.TID300Representation = TID300CobbAngle$1;
    Angle.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
        if (!TrackingIdentifier.includes(":")) {
            return false;
        }
        var _a = TrackingIdentifier.split(":"), cornerstone3DTag = _a[0], toolType = _a[1];
        if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
            return false;
        }
        return toolType === MEASUREMENT_TYPE$1;
    };
    return Angle;
}());
MeasurementReport.registerTool(Angle);

var TID300CobbAngle = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.CobbAngle;
var MEASUREMENT_TYPE = "CobbAngle";
var trackingIdentifierTextValue$4 = "".concat(CORNERSTONE_3D_TAG, ":").concat(MEASUREMENT_TYPE);
var CobbAngle = /** @class */ (function () {
    function CobbAngle() {
    }
    // TODO: this function is required for all Cornerstone Tool Adapters, since it is called by MeasurementReport.
    CobbAngle.getMeasurementData = function (MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
        var _a;
        var _b = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, CobbAngle.toolType), defaultState = _b.defaultState, NUMGroup = _b.NUMGroup, SCOORDGroup = _b.SCOORDGroup, ReferencedFrameNumber = _b.ReferencedFrameNumber;
        var referencedImageId = defaultState.annotation.metadata.referencedImageId;
        var GraphicData = SCOORDGroup.GraphicData;
        var worldCoords = [];
        for (var i = 0; i < GraphicData.length; i += 2) {
            var point = imageToWorldCoords(referencedImageId, [
                GraphicData[i],
                GraphicData[i + 1]
            ]);
            worldCoords.push(point);
        }
        var state = defaultState;
        state.annotation.data = {
            handles: {
                points: [
                    worldCoords[0],
                    worldCoords[1],
                    worldCoords[2],
                    worldCoords[3]
                ],
                activeHandleIndex: 0,
                textBox: {
                    hasMoved: false
                }
            },
            cachedStats: (_a = {},
                _a["imageId:".concat(referencedImageId)] = {
                    angle: NUMGroup
                        ? NUMGroup.MeasuredValueSequence.NumericValue
                        : null
                },
                _a),
            frameNumber: ReferencedFrameNumber
        };
        return state;
    };
    CobbAngle.getTID300RepresentationArguments = function (tool, worldToImageCoords) {
        var data = tool.data, finding = tool.finding, findingSites = tool.findingSites, metadata = tool.metadata;
        var _a = data.cachedStats, cachedStats = _a === void 0 ? {} : _a, handles = data.handles;
        var referencedImageId = metadata.referencedImageId;
        if (!referencedImageId) {
            throw new Error("CobbAngle.getTID300RepresentationArguments: referencedImageId is not defined");
        }
        var start1 = worldToImageCoords(referencedImageId, handles.points[0]);
        var end1 = worldToImageCoords(referencedImageId, handles.points[1]);
        var start2 = worldToImageCoords(referencedImageId, handles.points[2]);
        var end2 = worldToImageCoords(referencedImageId, handles.points[3]);
        var point1 = { x: start1[0], y: start1[1] };
        var point2 = { x: end1[0], y: end1[1] };
        var point3 = { x: start2[0], y: start2[1] };
        var point4 = { x: end2[0], y: end2[1] };
        var angle = (cachedStats["imageId:".concat(referencedImageId)] || {}).angle;
        return {
            point1: point1,
            point2: point2,
            point3: point3,
            point4: point4,
            rAngle: angle,
            trackingIdentifierTextValue: trackingIdentifierTextValue$4,
            finding: finding,
            findingSites: findingSites || []
        };
    };
    CobbAngle.toolType = MEASUREMENT_TYPE;
    CobbAngle.utilityToolType = MEASUREMENT_TYPE;
    CobbAngle.TID300Representation = TID300CobbAngle;
    CobbAngle.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
        if (!TrackingIdentifier.includes(":")) {
            return false;
        }
        var _a = TrackingIdentifier.split(":"), cornerstone3DTag = _a[0], toolType = _a[1];
        if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
            return false;
        }
        return toolType === MEASUREMENT_TYPE;
    };
    return CobbAngle;
}());
MeasurementReport.registerTool(CobbAngle);

function isValidCornerstoneTrackingIdentifier(trackingIdentifier) {
    if (!trackingIdentifier.includes(":")) {
        return false;
    }
    var _a = trackingIdentifier.split(":"), cornerstone3DTag = _a[0], toolType = _a[1];
    if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
        return false;
    }
    // The following is needed since the new cornerstone3D has changed
    // case names such as EllipticalRoi to EllipticalROI
    return toolType.toLowerCase() === this.toolType.toLowerCase();
}

var TID300Circle = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Circle;
var CIRCLEROI = "CircleROI";
var CircleROI = /** @class */ (function () {
    function CircleROI() {
    }
    /** Gets the measurement data for cornerstone, given DICOM SR measurement data. */
    CircleROI.getMeasurementData = function (MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
        var _a;
        var _b = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, CircleROI.toolType), defaultState = _b.defaultState, NUMGroup = _b.NUMGroup, SCOORDGroup = _b.SCOORDGroup, ReferencedFrameNumber = _b.ReferencedFrameNumber;
        var referencedImageId = defaultState.annotation.metadata.referencedImageId;
        var GraphicData = SCOORDGroup.GraphicData;
        // GraphicData is ordered as [centerX, centerY, endX, endY]
        var pointsWorld = [];
        for (var i = 0; i < GraphicData.length; i += 2) {
            var worldPos = imageToWorldCoords(referencedImageId, [
                GraphicData[i],
                GraphicData[i + 1]
            ]);
            pointsWorld.push(worldPos);
        }
        var state = defaultState;
        state.annotation.data = {
            handles: {
                points: __spreadArray([], pointsWorld, true),
                activeHandleIndex: 0,
                textBox: {
                    hasMoved: false
                }
            },
            cachedStats: (_a = {},
                _a["imageId:".concat(referencedImageId)] = {
                    area: NUMGroup
                        ? NUMGroup.MeasuredValueSequence.NumericValue
                        : 0,
                    // Dummy values to be updated by cornerstone
                    radius: 0,
                    perimeter: 0
                },
                _a),
            frameNumber: ReferencedFrameNumber
        };
        return state;
    };
    /**
     * Gets the TID 300 representation of a circle, given the cornerstone representation.
     *
     * @param {Object} tool
     * @returns
     */
    CircleROI.getTID300RepresentationArguments = function (tool, worldToImageCoords) {
        var data = tool.data, finding = tool.finding, findingSites = tool.findingSites, metadata = tool.metadata;
        var _a = data.cachedStats, cachedStats = _a === void 0 ? {} : _a, handles = data.handles;
        var referencedImageId = metadata.referencedImageId;
        if (!referencedImageId) {
            throw new Error("CircleROI.getTID300RepresentationArguments: referencedImageId is not defined");
        }
        var center = worldToImageCoords(referencedImageId, handles.points[0]);
        var end = worldToImageCoords(referencedImageId, handles.points[1]);
        var points = [];
        points.push({ x: center[0], y: center[1] });
        points.push({ x: end[0], y: end[1] });
        var _b = cachedStats["imageId:".concat(referencedImageId)] || {}, area = _b.area, radius = _b.radius;
        var perimeter = 2 * Math.PI * radius;
        return {
            area: area,
            perimeter: perimeter,
            radius: radius,
            points: points,
            trackingIdentifierTextValue: this.trackingIdentifierTextValue,
            finding: finding,
            findingSites: findingSites || []
        };
    };
    CircleROI.trackingIdentifierTextValue = "".concat(CORNERSTONE_3D_TAG, ":").concat(CIRCLEROI);
    CircleROI.toolType = CIRCLEROI;
    CircleROI.utilityToolType = CIRCLEROI;
    CircleROI.TID300Representation = TID300Circle;
    CircleROI.isValidCornerstoneTrackingIdentifier = isValidCornerstoneTrackingIdentifier;
    return CircleROI;
}());
MeasurementReport.registerTool(CircleROI);

var TID300Ellipse = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Ellipse;
var ELLIPTICALROI = "EllipticalROI";
var EPSILON = 1e-4;
var EllipticalROI = /** @class */ (function () {
    function EllipticalROI() {
    }
    EllipticalROI.getMeasurementData = function (MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
        var _a;
        var _b = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, EllipticalROI.toolType), defaultState = _b.defaultState, NUMGroup = _b.NUMGroup, SCOORDGroup = _b.SCOORDGroup, ReferencedFrameNumber = _b.ReferencedFrameNumber;
        var referencedImageId = defaultState.annotation.metadata.referencedImageId;
        var GraphicData = SCOORDGroup.GraphicData;
        // GraphicData is ordered as [majorAxisStartX, majorAxisStartY, majorAxisEndX, majorAxisEndY, minorAxisStartX, minorAxisStartY, minorAxisEndX, minorAxisEndY]
        // But Cornerstone3D points are ordered as top, bottom, left, right for the
        // ellipse so we need to identify if the majorAxis is horizontal or vertical
        // in the image plane and then choose the correct points to use for the ellipse.
        var pointsWorld = [];
        for (var i = 0; i < GraphicData.length; i += 2) {
            var worldPos = imageToWorldCoords(referencedImageId, [
                GraphicData[i],
                GraphicData[i + 1]
            ]);
            pointsWorld.push(worldPos);
        }
        var majorAxisStart = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.fromValues */ .R3.fromValues.apply(gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3 */ .R3, pointsWorld[0]);
        var majorAxisEnd = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.fromValues */ .R3.fromValues.apply(gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3 */ .R3, pointsWorld[1]);
        var minorAxisStart = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.fromValues */ .R3.fromValues.apply(gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3 */ .R3, pointsWorld[2]);
        var minorAxisEnd = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.fromValues */ .R3.fromValues.apply(gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3 */ .R3, pointsWorld[3]);
        var majorAxisVec = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.create */ .R3.create();
        gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.sub */ .R3.sub(majorAxisVec, majorAxisEnd, majorAxisStart);
        // normalize majorAxisVec to avoid scaling issues
        gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.normalize */ .R3.normalize(majorAxisVec, majorAxisVec);
        var minorAxisVec = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.create */ .R3.create();
        gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.sub */ .R3.sub(minorAxisVec, minorAxisEnd, minorAxisStart);
        gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.normalize */ .R3.normalize(minorAxisVec, minorAxisVec);
        var imagePlaneModule = metadata.get("imagePlaneModule", referencedImageId);
        if (!imagePlaneModule) {
            throw new Error("imageId does not have imagePlaneModule metadata");
        }
        var columnCosines = imagePlaneModule.columnCosines;
        // find which axis is parallel to the columnCosines
        var columnCosinesVec = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.fromValues */ .R3.fromValues(columnCosines[0], columnCosines[1], columnCosines[2]);
        var projectedMajorAxisOnColVec = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.dot */ .R3.dot(columnCosinesVec, majorAxisVec);
        var projectedMinorAxisOnColVec = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.dot */ .R3.dot(columnCosinesVec, minorAxisVec);
        var absoluteOfMajorDotProduct = Math.abs(projectedMajorAxisOnColVec);
        var absoluteOfMinorDotProduct = Math.abs(projectedMinorAxisOnColVec);
        var ellipsePoints = [];
        if (Math.abs(absoluteOfMajorDotProduct - 1) < EPSILON) {
            ellipsePoints = [
                pointsWorld[0],
                pointsWorld[1],
                pointsWorld[2],
                pointsWorld[3]
            ];
        }
        else if (Math.abs(absoluteOfMinorDotProduct - 1) < EPSILON) {
            ellipsePoints = [
                pointsWorld[2],
                pointsWorld[3],
                pointsWorld[0],
                pointsWorld[1]
            ];
        }
        else {
            console.warn("OBLIQUE ELLIPSE NOT YET SUPPORTED");
        }
        var state = defaultState;
        state.annotation.data = {
            handles: {
                points: __spreadArray([], ellipsePoints, true),
                activeHandleIndex: 0,
                textBox: {
                    hasMoved: false
                }
            },
            cachedStats: (_a = {},
                _a["imageId:".concat(referencedImageId)] = {
                    area: NUMGroup
                        ? NUMGroup.MeasuredValueSequence.NumericValue
                        : 0
                },
                _a),
            frameNumber: ReferencedFrameNumber
        };
        return state;
    };
    EllipticalROI.getTID300RepresentationArguments = function (tool, worldToImageCoords) {
        var data = tool.data, finding = tool.finding, findingSites = tool.findingSites, metadata = tool.metadata;
        var _a = data.cachedStats, cachedStats = _a === void 0 ? {} : _a, handles = data.handles;
        var rotation = data.initialRotation || 0;
        var referencedImageId = metadata.referencedImageId;
        if (!referencedImageId) {
            throw new Error("EllipticalROI.getTID300RepresentationArguments: referencedImageId is not defined");
        }
        var top, bottom, left, right;
        // this way when it's restored we can assume the initial rotation is 0.
        if (rotation == 90 || rotation == 270) {
            bottom = worldToImageCoords(referencedImageId, handles.points[2]);
            top = worldToImageCoords(referencedImageId, handles.points[3]);
            left = worldToImageCoords(referencedImageId, handles.points[0]);
            right = worldToImageCoords(referencedImageId, handles.points[1]);
        }
        else {
            top = worldToImageCoords(referencedImageId, handles.points[0]);
            bottom = worldToImageCoords(referencedImageId, handles.points[1]);
            left = worldToImageCoords(referencedImageId, handles.points[2]);
            right = worldToImageCoords(referencedImageId, handles.points[3]);
        }
        // find the major axis and minor axis
        var topBottomLength = Math.abs(top[1] - bottom[1]);
        var leftRightLength = Math.abs(left[0] - right[0]);
        var points = [];
        if (topBottomLength > leftRightLength) {
            // major axis is bottom to top
            points.push({ x: top[0], y: top[1] });
            points.push({ x: bottom[0], y: bottom[1] });
            // minor axis is left to right
            points.push({ x: left[0], y: left[1] });
            points.push({ x: right[0], y: right[1] });
        }
        else {
            // major axis is left to right
            points.push({ x: left[0], y: left[1] });
            points.push({ x: right[0], y: right[1] });
            // minor axis is bottom to top
            points.push({ x: top[0], y: top[1] });
            points.push({ x: bottom[0], y: bottom[1] });
        }
        var area = (cachedStats["imageId:".concat(referencedImageId)] || {}).area;
        return {
            area: area,
            points: points,
            trackingIdentifierTextValue: this.trackingIdentifierTextValue,
            finding: finding,
            findingSites: findingSites || []
        };
    };
    EllipticalROI.trackingIdentifierTextValue = "".concat(CORNERSTONE_3D_TAG, ":").concat(ELLIPTICALROI);
    EllipticalROI.toolType = ELLIPTICALROI;
    EllipticalROI.utilityToolType = ELLIPTICALROI;
    EllipticalROI.TID300Representation = TID300Ellipse;
    EllipticalROI.isValidCornerstoneTrackingIdentifier = isValidCornerstoneTrackingIdentifier;
    return EllipticalROI;
}());
MeasurementReport.registerTool(EllipticalROI);

var TID300Polyline$1 = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Polyline;
var TOOLTYPE = "RectangleROI";
var trackingIdentifierTextValue$3 = "".concat(CORNERSTONE_3D_TAG, ":").concat(TOOLTYPE);
var RectangleROI = /** @class */ (function () {
    function RectangleROI() {
    }
    RectangleROI.getMeasurementData = function (MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
        var _a;
        var _b = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, RectangleROI.toolType), defaultState = _b.defaultState, NUMGroup = _b.NUMGroup, SCOORDGroup = _b.SCOORDGroup, ReferencedFrameNumber = _b.ReferencedFrameNumber;
        var referencedImageId = defaultState.annotation.metadata.referencedImageId;
        var GraphicData = SCOORDGroup.GraphicData;
        var worldCoords = [];
        for (var i = 0; i < GraphicData.length; i += 2) {
            var point = imageToWorldCoords(referencedImageId, [
                GraphicData[i],
                GraphicData[i + 1]
            ]);
            worldCoords.push(point);
        }
        var state = defaultState;
        state.annotation.data = {
            handles: {
                points: [
                    worldCoords[0],
                    worldCoords[1],
                    worldCoords[3],
                    worldCoords[2]
                ],
                activeHandleIndex: 0,
                textBox: {
                    hasMoved: false
                }
            },
            cachedStats: (_a = {},
                _a["imageId:".concat(referencedImageId)] = {
                    area: NUMGroup
                        ? NUMGroup.MeasuredValueSequence.NumericValue
                        : null
                },
                _a),
            frameNumber: ReferencedFrameNumber
        };
        return state;
    };
    RectangleROI.getTID300RepresentationArguments = function (tool, worldToImageCoords) {
        var data = tool.data, finding = tool.finding, findingSites = tool.findingSites, metadata = tool.metadata;
        var _a = data.cachedStats, cachedStats = _a === void 0 ? {} : _a, handles = data.handles;
        var referencedImageId = metadata.referencedImageId;
        if (!referencedImageId) {
            throw new Error("CobbAngle.getTID300RepresentationArguments: referencedImageId is not defined");
        }
        var corners = handles.points.map(function (point) {
            return worldToImageCoords(referencedImageId, point);
        });
        var area = cachedStats.area, perimeter = cachedStats.perimeter;
        return {
            points: [
                corners[0],
                corners[1],
                corners[3],
                corners[2],
                corners[0]
            ],
            area: area,
            perimeter: perimeter,
            trackingIdentifierTextValue: trackingIdentifierTextValue$3,
            finding: finding,
            findingSites: findingSites || []
        };
    };
    RectangleROI.toolType = TOOLTYPE;
    RectangleROI.utilityToolType = TOOLTYPE;
    RectangleROI.TID300Representation = TID300Polyline$1;
    RectangleROI.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
        if (!TrackingIdentifier.includes(":")) {
            return false;
        }
        var _a = TrackingIdentifier.split(":"), cornerstone3DTag = _a[0], toolType = _a[1];
        if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
            return false;
        }
        return toolType === TOOLTYPE;
    };
    return RectangleROI;
}());
MeasurementReport.registerTool(RectangleROI);

var TID300Length = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Length;
var LENGTH = "Length";
var trackingIdentifierTextValue$2 = "".concat(CORNERSTONE_3D_TAG, ":").concat(LENGTH);
var Length = /*#__PURE__*/function () {
  function Length() {
    _classCallCheck(this, Length);
  }
  _createClass(Length, null, [{
    key: "getMeasurementData",
    value:
    // TODO: this function is required for all Cornerstone Tool Adapters, since it is called by MeasurementReport.
    function getMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
      var _MeasurementReport$ge = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, Length.toolType),
        defaultState = _MeasurementReport$ge.defaultState,
        NUMGroup = _MeasurementReport$ge.NUMGroup,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup,
        ReferencedFrameNumber = _MeasurementReport$ge.ReferencedFrameNumber;
      var referencedImageId = defaultState.annotation.metadata.referencedImageId;
      var GraphicData = SCOORDGroup.GraphicData;
      var worldCoords = [];
      for (var i = 0; i < GraphicData.length; i += 2) {
        var point = imageToWorldCoords(referencedImageId, [GraphicData[i], GraphicData[i + 1]]);
        worldCoords.push(point);
      }
      var state = defaultState;
      state.annotation.data = {
        handles: {
          points: [worldCoords[0], worldCoords[1]],
          activeHandleIndex: 0,
          textBox: {
            hasMoved: false
          }
        },
        cachedStats: _defineProperty({}, "imageId:".concat(referencedImageId), {
          length: NUMGroup ? NUMGroup.MeasuredValueSequence.NumericValue : 0
        }),
        frameNumber: ReferencedFrameNumber
      };
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool, worldToImageCoords) {
      var data = tool.data,
        finding = tool.finding,
        findingSites = tool.findingSites,
        metadata = tool.metadata;
      var _data$cachedStats = data.cachedStats,
        cachedStats = _data$cachedStats === void 0 ? {} : _data$cachedStats,
        handles = data.handles;
      var referencedImageId = metadata.referencedImageId;
      if (!referencedImageId) {
        throw new Error("Length.getTID300RepresentationArguments: referencedImageId is not defined");
      }
      var start = worldToImageCoords(referencedImageId, handles.points[0]);
      var end = worldToImageCoords(referencedImageId, handles.points[1]);
      var point1 = {
        x: start[0],
        y: start[1]
      };
      var point2 = {
        x: end[0],
        y: end[1]
      };
      var _ref = cachedStats["imageId:".concat(referencedImageId)] || {},
        distance = _ref.length;
      return {
        point1: point1,
        point2: point2,
        distance: distance,
        trackingIdentifierTextValue: trackingIdentifierTextValue$2,
        finding: finding,
        findingSites: findingSites || []
      };
    }
  }]);
  return Length;
}();
Length.toolType = LENGTH;
Length.utilityToolType = LENGTH;
Length.TID300Representation = TID300Length;
Length.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone3DTag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
    return false;
  }
  return toolType === LENGTH;
};
MeasurementReport.registerTool(Length);

var TID300Polyline = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Polyline;
var PLANARFREEHANDROI = "PlanarFreehandROI";
var trackingIdentifierTextValue$1 = "".concat(CORNERSTONE_3D_TAG, ":").concat(PLANARFREEHANDROI);
var closedContourThreshold = 1e-5;
var PlanarFreehandROI = /** @class */ (function () {
    function PlanarFreehandROI() {
    }
    PlanarFreehandROI.getMeasurementData = function (MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
        var _a = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, PlanarFreehandROI.toolType), defaultState = _a.defaultState, SCOORDGroup = _a.SCOORDGroup, ReferencedFrameNumber = _a.ReferencedFrameNumber;
        var referencedImageId = defaultState.annotation.metadata.referencedImageId;
        var GraphicData = SCOORDGroup.GraphicData;
        var worldCoords = [];
        for (var i = 0; i < GraphicData.length; i += 2) {
            var point = imageToWorldCoords(referencedImageId, [
                GraphicData[i],
                GraphicData[i + 1]
            ]);
            worldCoords.push(point);
        }
        var distanceBetweenFirstAndLastPoint = gl_matrix__WEBPACK_IMPORTED_MODULE_4__/* .vec3.distance */ .R3.distance(worldCoords[worldCoords.length - 1], worldCoords[0]);
        var isOpenContour = true;
        // If the contour is closed, this should have been encoded as exactly the same point, so check for a very small difference.
        if (distanceBetweenFirstAndLastPoint < closedContourThreshold) {
            worldCoords.pop(); // Remove the last element which is duplicated.
            isOpenContour = false;
        }
        var points = [];
        if (isOpenContour) {
            points.push(worldCoords[0], worldCoords[worldCoords.length - 1]);
        }
        var state = defaultState;
        state.annotation.data = {
            polyline: worldCoords,
            isOpenContour: isOpenContour,
            handles: {
                points: points,
                activeHandleIndex: null,
                textBox: {
                    hasMoved: false
                }
            },
            frameNumber: ReferencedFrameNumber
        };
        return state;
    };
    PlanarFreehandROI.getTID300RepresentationArguments = function (tool, worldToImageCoords) {
        var data = tool.data, finding = tool.finding, findingSites = tool.findingSites, metadata = tool.metadata;
        var isOpenContour = data.isOpenContour, polyline = data.polyline;
        var referencedImageId = metadata.referencedImageId;
        if (!referencedImageId) {
            throw new Error("PlanarFreehandROI.getTID300RepresentationArguments: referencedImageId is not defined");
        }
        var points = polyline.map(function (worldPos) {
            return worldToImageCoords(referencedImageId, worldPos);
        });
        if (!isOpenContour) {
            // Need to repeat the first point at the end of to have an explicitly closed contour.
            var firstPoint = points[0];
            // Explicitly expand to avoid ciruclar references.
            points.push([firstPoint[0], firstPoint[1]]);
        }
        var area = 0; // TODO -> The tool doesn't have these stats yet.
        var perimeter = 0;
        return {
            points: points,
            area: area,
            perimeter: perimeter,
            trackingIdentifierTextValue: trackingIdentifierTextValue$1,
            finding: finding,
            findingSites: findingSites || []
        };
    };
    PlanarFreehandROI.toolType = PLANARFREEHANDROI;
    PlanarFreehandROI.utilityToolType = PLANARFREEHANDROI;
    PlanarFreehandROI.TID300Representation = TID300Polyline;
    PlanarFreehandROI.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
        if (!TrackingIdentifier.includes(":")) {
            return false;
        }
        var _a = TrackingIdentifier.split(":"), cornerstone3DTag = _a[0], toolType = _a[1];
        if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
            return false;
        }
        return toolType === PLANARFREEHANDROI;
    };
    return PlanarFreehandROI;
}());
MeasurementReport.registerTool(PlanarFreehandROI);

var TID300Point = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .utilities */ .hC.TID300.Point;
var PROBE = "Probe";
var trackingIdentifierTextValue = "".concat(CORNERSTONE_3D_TAG, ":").concat(PROBE);
var Probe = /*#__PURE__*/function () {
  function Probe() {
    _classCallCheck(this, Probe);
  }
  _createClass(Probe, null, [{
    key: "getMeasurementData",
    value: function getMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, imageToWorldCoords, metadata) {
      var _MeasurementReport$ge = MeasurementReport.getSetupMeasurementData(MeasurementGroup, sopInstanceUIDToImageIdMap, metadata, Probe.toolType),
        defaultState = _MeasurementReport$ge.defaultState,
        SCOORDGroup = _MeasurementReport$ge.SCOORDGroup,
        ReferencedFrameNumber = _MeasurementReport$ge.ReferencedFrameNumber;
      var referencedImageId = defaultState.annotation.metadata.referencedImageId;
      var GraphicData = SCOORDGroup.GraphicData;
      var worldCoords = [];
      for (var i = 0; i < GraphicData.length; i += 2) {
        var point = imageToWorldCoords(referencedImageId, [GraphicData[i], GraphicData[i + 1]]);
        worldCoords.push(point);
      }
      var state = defaultState;
      state.annotation.data = {
        handles: {
          points: worldCoords,
          activeHandleIndex: null,
          textBox: {
            hasMoved: false
          }
        },
        frameNumber: ReferencedFrameNumber
      };
      return state;
    }
  }, {
    key: "getTID300RepresentationArguments",
    value: function getTID300RepresentationArguments(tool, worldToImageCoords) {
      var data = tool.data,
        metadata = tool.metadata;
      var finding = tool.finding,
        findingSites = tool.findingSites;
      var referencedImageId = metadata.referencedImageId;
      if (!referencedImageId) {
        throw new Error("Probe.getTID300RepresentationArguments: referencedImageId is not defined");
      }
      var points = data.handles.points;
      var pointsImage = points.map(function (point) {
        var pointImage = worldToImageCoords(referencedImageId, point);
        return {
          x: pointImage[0],
          y: pointImage[1]
        };
      });
      var TID300RepresentationArguments = {
        points: pointsImage,
        trackingIdentifierTextValue: trackingIdentifierTextValue,
        findingSites: findingSites || [],
        finding: finding
      };
      return TID300RepresentationArguments;
    }
  }]);
  return Probe;
}();
Probe.toolType = PROBE;
Probe.utilityToolType = PROBE;
Probe.TID300Representation = TID300Point;
Probe.isValidCornerstoneTrackingIdentifier = function (TrackingIdentifier) {
  if (!TrackingIdentifier.includes(":")) {
    return false;
  }
  var _TrackingIdentifier$s = TrackingIdentifier.split(":"),
    _TrackingIdentifier$s2 = _slicedToArray(_TrackingIdentifier$s, 2),
    cornerstone3DTag = _TrackingIdentifier$s2[0],
    toolType = _TrackingIdentifier$s2[1];
  if (cornerstone3DTag !== CORNERSTONE_3D_TAG) {
    return false;
  }
  return toolType === PROBE;
};
MeasurementReport.registerTool(Probe);

var Normalizer = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .normalizers */ .oq.Normalizer;
var SegmentationDerivation = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .derivations */ .U7.Segmentation;
/**
 * generateSegmentation - Generates a DICOM Segmentation object given cornerstoneTools data.
 *
 * @param images - An array of the cornerstone image objects, which includes imageId and metadata
 * @param labelmaps - An array of the 3D Volumes that contain the segmentation data.
 */
function generateSegmentation(images, labelmaps, metadata, options) {
    if (options === void 0) { options = {}; }
    var segmentation = _createMultiframeSegmentationFromReferencedImages(images, metadata, options);
    return fillSegmentation$1(segmentation, labelmaps, options);
}
/**
 * _createMultiframeSegmentationFromReferencedImages - description
 *
 * @param images - An array of the cornerstone image objects related to the reference
 * series that the segmentation is derived from. You can use methods such as
 * volume.getCornerstoneImages() to get this array.
 *
 * @param options - the options object for the SegmentationDerivation.
 * @returns The Seg derived dataSet.
 */
function _createMultiframeSegmentationFromReferencedImages(images, metadata, options) {
    var datasets = images.map(function (image) {
        // add the sopClassUID to the dataset
        var instance = metadata.get("instance", image.imageId);
        return __assign(__assign(__assign({}, image), instance), { 
            // Todo: move to dcmjs tag style
            SOPClassUID: instance.SopClassUID || instance.SOPClassUID, SOPInstanceUID: instance.SopInstanceUID || instance.SOPInstanceUID, PixelData: image.getPixelData(), _vrMap: {
                PixelData: "OW"
            }, _meta: {} });
    });
    var multiframe = Normalizer.normalizeToDataset(datasets);
    return new SegmentationDerivation([multiframe], options);
}

/**
 * Generates 2D label maps from a 3D label map.
 * @param labelmap3D - The 3D label map object to generate 2D label maps from. It is derived
 * from the volume labelmap.
 * @returns The label map object containing the 2D label maps and segments on label maps.
 */
function generateLabelMaps2DFrom3D(labelmap3D) {
    // 1. we need to generate labelmaps2D from labelmaps3D, a labelmap2D is for each
    // slice
    var scalarData = labelmap3D.scalarData, dimensions = labelmap3D.dimensions;
    // scalarData is a flat array of all the pixels in the volume.
    var labelmaps2D = [];
    var segmentsOnLabelmap3D = new Set();
    // X-Y are the row and column dimensions, Z is the number of slices.
    for (var z = 0; z < dimensions[2]; z++) {
        var pixelData = scalarData.slice(z * dimensions[0] * dimensions[1], (z + 1) * dimensions[0] * dimensions[1]);
        var segmentsOnLabelmap = [];
        for (var i = 0; i < pixelData.length; i++) {
            var segment = pixelData[i];
            if (!segmentsOnLabelmap.includes(segment) && segment !== 0) {
                segmentsOnLabelmap.push(segment);
            }
        }
        var labelmap2D = {
            segmentsOnLabelmap: segmentsOnLabelmap,
            pixelData: pixelData,
            rows: dimensions[1],
            columns: dimensions[0]
        };
        if (segmentsOnLabelmap.length === 0) {
            continue;
        }
        segmentsOnLabelmap.forEach(function (segmentIndex) {
            segmentsOnLabelmap3D.add(segmentIndex);
        });
        labelmaps2D[dimensions[2] - 1 - z] = labelmap2D;
    }
    // remove segment 0 from segmentsOnLabelmap3D
    labelmap3D.segmentsOnLabelmap = Array.from(segmentsOnLabelmap3D);
    labelmap3D.labelmaps2D = labelmaps2D;
    return labelmap3D;
}

var Segmentation$2 = CornerstoneSEG.Segmentation;
var generateToolStateCornerstoneLegacy = Segmentation$2.generateToolState;
/**
 * generateToolState - Given a set of cornerstoneTools imageIds and a Segmentation buffer,
 * derive cornerstoneTools toolState and brush metadata.
 *
 * @param   imageIds - An array of the imageIds.
 * @param   arrayBuffer - The SEG arrayBuffer.
 * @param   skipOverlapping - skip checks for overlapping segs, default value false.
 * @param   tolerance - default value 1.e-3.
 *
 * @returns a list of array buffer for each labelMap
 *  an object from which the segment metadata can be derived
 *  list containing the track of segments per frame
 *  list containing the track of segments per frame for each labelMap                   (available only for the overlapping case).
 */
function generateToolState(imageIds, arrayBuffer, metadataProvider, skipOverlapping, tolerance) {
    if (skipOverlapping === void 0) { skipOverlapping = false; }
    if (tolerance === void 0) { tolerance = 1e-3; }
    return generateToolStateCornerstoneLegacy(imageIds, arrayBuffer, metadataProvider, skipOverlapping, tolerance);
}

var Segmentation$1 = /*#__PURE__*/Object.freeze({
  __proto__: null,
  generateLabelMaps2DFrom3D: generateLabelMaps2DFrom3D,
  generateSegmentation: generateSegmentation,
  generateToolState: generateToolState
});

/**
 * Checks if point is within array
 * @param {*} array
 * @param {*} pt
 * @returns
 */
function ptInArray(array, pt) {
  var index = -1;
  for (var i = 0; i < array.length; i++) {
    if (isSamePoint(pt, array[i])) {
      index = i;
    }
  }
  return index;
}

/**
 * Checks if point A and point B contain same values
 * @param {*} ptA
 * @param {*} ptB
 * @returns
 */
function isSamePoint(ptA, ptB) {
  if (ptA[0] == ptB[0] && ptA[1] == ptB[1] && ptA[2] == ptB[2]) {
    return true;
  } else {
    return false;
  }
}

/**
 * Goes through linesArray and replaces all references of old index with new index
 * @param {*} linesArray
 * @param {*} oldIndex
 * @param {*} newIndex
 */
function replacePointIndexReferences(linesArray, oldIndex, newIndex) {
  for (var i = 0; i < linesArray.length; i++) {
    var line = linesArray[i];
    if (line.a == oldIndex) {
      line.a = newIndex;
    } else if (line.b == oldIndex) {
      line.b = newIndex;
    }
  }
}

/**
 * Iterate through polyData from vtkjs and merge any points that are the same
 * then update merged point references within lines array
 * @param {*} polyData
 * @param {*} bypass
 * @returns
 */
function removeDuplicatePoints(polyData, bypass) {
  var points = polyData.getPoints();
  var lines = polyData.getLines();
  var pointsArray = [];
  for (var i = 0; i < points.getNumberOfPoints(); i++) {
    var pt = points.getPoint(i).slice();
    pointsArray.push(pt);
  }
  var linesArray = [];
  for (var _i = 0; _i < lines.getNumberOfCells(); _i++) {
    var cell = lines.getCell(_i * 3).slice();
    //console.log(JSON.stringify(cell));
    var a = cell[0];
    var b = cell[1];
    var line = {
      a: a,
      b: b
    };
    linesArray.push(line);
  }
  if (bypass) {
    return {
      points: pointsArray,
      lines: linesArray
    };
  }

  // Iterate through points and replace any duplicates
  var newPoints = [];
  for (var _i2 = 0; _i2 < pointsArray.length; _i2++) {
    var _pt = pointsArray[_i2];
    var index = ptInArray(newPoints, _pt);
    if (index >= 0) {
      // Duplicate Point -> replace references in lines
      replacePointIndexReferences(linesArray, _i2, index);
    } else {
      index = newPoints.length;
      newPoints.push(_pt);
      replacePointIndexReferences(linesArray, _i2, index);
    }
  }

  // Final pass through lines, remove any that refer to exact same point
  var newLines = [];
  linesArray.forEach(function (line) {
    if (line.a != line.b) {
      newLines.push(line);
    }
  });
  return {
    points: newPoints,
    lines: newLines
  };
}

function findNextLink(line, lines, contourPoints) {
  var index = -1;
  lines.forEach(function (cell, i) {
    if (index >= 0) {
      return;
    }
    if (cell.a == line.b) {
      index = i;
    }
  });
  if (index >= 0) {
    var nextLine = lines[index];
    lines.splice(index, 1);
    contourPoints.push(nextLine.b);
    if (contourPoints[0] == nextLine.b) {
      return {
        remainingLines: lines,
        contourPoints: contourPoints,
        type: "CLOSED_PLANAR"
        //type: 'CLOSEDPLANAR_XOR',
      };
    }

    return findNextLink(nextLine, lines, contourPoints);
  }
  return {
    remainingLines: lines,
    contourPoints: contourPoints,
    type: "OPEN_PLANAR"
  };
}

/**
 *
 * @param {*} lines
 */
function findContours(lines) {
  if (lines.length == 0) {
    return [];
  }
  var contourPoints = [];
  var firstCell = lines.shift();
  contourPoints.push(firstCell.a);
  contourPoints.push(firstCell.b);
  var result = findNextLink(firstCell, lines, contourPoints);
  if (result.remainingLines.length == 0) {
    return [{
      type: result.type,
      contourPoints: result.contourPoints
    }];
  } else {
    var extraContours = findContours(result.remainingLines);
    extraContours.push({
      type: result.type,
      contourPoints: result.contourPoints
    });
    return extraContours;
  }
}
function findContoursFromReducedSet(lines, points) {
  return findContours(lines);
}

function generateContourSetsFromLabelmap(_ref) {
  var segmentations = _ref.segmentations,
    cornerstoneCache = _ref.cornerstoneCache,
    cornerstoneToolsEnums = _ref.cornerstoneToolsEnums,
    vtkUtils = _ref.vtkUtils;
  var LABELMAP = cornerstoneToolsEnums.SegmentationRepresentations.Labelmap;
  var representationData = segmentations.representationData,
    segments = segmentations.segments;
  var segVolumeId = representationData[LABELMAP].volumeId;

  // Get segmentation volume
  var vol = cornerstoneCache.getVolume(segVolumeId);
  if (!vol) {
    console.warn("No volume found for ".concat(segVolumeId));
    return;
  }
  var numSlices = vol.dimensions[2];

  // Get image volume segmentation references
  var imageVol = cornerstoneCache.getVolume(vol.referencedVolumeId);
  if (!imageVol) {
    console.warn("No volume found for ".concat(vol.referencedVolumeId));
    return;
  }

  // NOTE: Workaround for marching squares not finding closed contours at
  // boundary of image volume, clear pixels along x-y border of volume
  var segData = vol.imageData.getPointData().getScalars().getData();
  var pixelsPerSlice = vol.dimensions[0] * vol.dimensions[1];
  for (var z = 0; z < numSlices; z++) {
    for (var y = 0; y < vol.dimensions[1]; y++) {
      for (var x = 0; x < vol.dimensions[0]; x++) {
        var index = x + y * vol.dimensions[0] + z * pixelsPerSlice;
        if (x === 0 || y === 0 || x === vol.dimensions[0] - 1 || y === vol.dimensions[1] - 1) {
          segData[index] = 0;
        }
      }
    }
  }

  // end workaround
  //
  //
  var ContourSets = [];

  // Iterate through all segments in current segmentation set
  var numSegments = segments.length;
  for (var segIndex = 0; segIndex < numSegments; segIndex++) {
    var segment = segments[segIndex];

    // Skip empty segments
    if (!segment) {
      continue;
    }
    var contourSequence = [];
    for (var sliceIndex = 0; sliceIndex < numSlices; sliceIndex++) {
      // Check if the slice is empty before running marching cube
      if (isSliceEmptyForSegment(sliceIndex, segData, pixelsPerSlice, segIndex)) {
        continue;
      }
      try {
        var _reducedSet$points;
        var scalars = vtkUtils.vtkDataArray.newInstance({
          name: "Scalars",
          values: Array.from(segData),
          numberOfComponents: 1
        });

        // Modify segData for this specific segment directly
        var segmentIndexFound = false;
        for (var i = 0; i < segData.length; i++) {
          var value = segData[i];
          if (value === segIndex) {
            segmentIndexFound = true;
            scalars.setValue(i, 1);
          } else {
            scalars.setValue(i, 0);
          }
        }
        if (!segmentIndexFound) {
          continue;
        }
        var mSquares = vtkUtils.vtkImageMarchingSquares.newInstance({
          slice: sliceIndex
        });

        // filter out the scalar data so that only it has background and
        // the current segment index
        var imageDataCopy = vtkUtils.vtkImageData.newInstance();
        imageDataCopy.shallowCopy(vol.imageData);
        imageDataCopy.getPointData().setScalars(scalars);

        // Connect pipeline
        mSquares.setInputData(imageDataCopy);
        var cValues = [];
        cValues[0] = 1;
        mSquares.setContourValues(cValues);
        mSquares.setMergePoints(false);

        // Perform marching squares
        var msOutput = mSquares.getOutputData();

        // Clean up output from marching squares
        var reducedSet = removeDuplicatePoints(msOutput);
        if ((_reducedSet$points = reducedSet.points) !== null && _reducedSet$points !== void 0 && _reducedSet$points.length) {
          var contours = findContoursFromReducedSet(reducedSet.lines, reducedSet.points);
          contourSequence.push({
            referencedImageId: imageVol.imageIds[sliceIndex],
            contours: contours,
            polyData: reducedSet
          });
        }
      } catch (e) {
        console.warn(sliceIndex);
        console.warn(e);
      }
    }
    var metadata = {
      referencedImageId: imageVol.imageIds[0],
      // just use 0
      FrameOfReferenceUID: imageVol.metadata.FrameOfReferenceUID
    };
    var ContourSet = {
      label: segment.label,
      color: segment.color,
      metadata: metadata,
      sliceContours: contourSequence
    };
    ContourSets.push(ContourSet);
  }
  return ContourSets;
}
function isSliceEmptyForSegment(sliceIndex, segData, pixelsPerSlice, segIndex) {
  var startIdx = sliceIndex * pixelsPerSlice;
  var endIdx = startIdx + pixelsPerSlice;
  for (var i = startIdx; i < endIdx; i++) {
    if (segData[i] === segIndex) {
      return false;
    }
  }
  return true;
}

// comment
var RectangleROIStartEndThreshold = /*#__PURE__*/function () {
  function RectangleROIStartEndThreshold() {
    _classCallCheck(this, RectangleROIStartEndThreshold);
  } // empty
  _createClass(RectangleROIStartEndThreshold, null, [{
    key: "getContourSequence",
    value: function getContourSequence(toolData, metadataProvider) {
      var data = toolData.data;
      var _data$cachedStats = data.cachedStats,
        projectionPoints = _data$cachedStats.projectionPoints,
        projectionPointsImageIds = _data$cachedStats.projectionPointsImageIds;
      return projectionPoints.map(function (point, index) {
        var ContourData = getPointData(point);
        var ContourImageSequence = getContourImageSequence(projectionPointsImageIds[index], metadataProvider);
        return {
          NumberOfContourPoints: ContourData.length / 3,
          ContourImageSequence: ContourImageSequence,
          ContourGeometricType: "CLOSED_PLANAR",
          ContourData: ContourData
        };
      });
    }
  }]);
  return RectangleROIStartEndThreshold;
}();
RectangleROIStartEndThreshold.toolName = "RectangleROIStartEndThreshold";
function getPointData(points) {
  // Since this is a closed contour, the order of the points is important.
  // re-order the points to be in the correct order clockwise
  // Spread to make sure Float32Arrays are converted to arrays
  var orderedPoints = [].concat(_toConsumableArray(points[0]), _toConsumableArray(points[1]), _toConsumableArray(points[3]), _toConsumableArray(points[2]));
  var pointsArray = orderedPoints.flat();

  // reduce the precision of the points to 2 decimal places
  var pointsArrayWithPrecision = pointsArray.map(function (point) {
    return point.toFixed(2);
  });
  return pointsArrayWithPrecision;
}
function getContourImageSequence(imageId, metadataProvider) {
  var sopCommon = metadataProvider.get("sopCommonModule", imageId);
  return {
    ReferencedSOPClassUID: sopCommon.sopClassUID,
    ReferencedSOPInstanceUID: sopCommon.sopInstanceUID
  };
}

function validateAnnotation(annotation) {
  if (!(annotation !== null && annotation !== void 0 && annotation.data)) {
    throw new Error("Tool data is empty");
  }
  if (!annotation.metadata || annotation.metadata.referenceImageId) {
    throw new Error("Tool data is not associated with any imageId");
  }
}
var AnnotationToPointData = /*#__PURE__*/function () {
  function AnnotationToPointData() {
    _classCallCheck(this, AnnotationToPointData);
  } // empty
  _createClass(AnnotationToPointData, null, [{
    key: "convert",
    value: function convert(annotation, index, metadataProvider) {
      validateAnnotation(annotation);
      var toolName = annotation.metadata.toolName;
      var toolClass = AnnotationToPointData.TOOL_NAMES[toolName];
      if (!toolClass) {
        throw new Error("Unknown tool type: ".concat(toolName, ", cannot convert to RTSSReport"));
      }

      // Each toolData should become a list of contours, ContourSequence
      // contains a list of contours with their pointData, their geometry
      // type and their length.
      var ContourSequence = toolClass.getContourSequence(annotation, metadataProvider);

      // Todo: random rgb color for now, options should be passed in
      var color = [Math.floor(Math.random() * 255), Math.floor(Math.random() * 255), Math.floor(Math.random() * 255)];
      return {
        ReferencedROINumber: index + 1,
        ROIDisplayColor: color,
        ContourSequence: ContourSequence
      };
    }
  }, {
    key: "register",
    value: function register(toolClass) {
      AnnotationToPointData.TOOL_NAMES[toolClass.toolName] = toolClass;
    }
  }]);
  return AnnotationToPointData;
}();
AnnotationToPointData.TOOL_NAMES = {};
AnnotationToPointData.register(RectangleROIStartEndThreshold);

function getPatientModule(imageId, metadataProvider) {
  var generalSeriesModule = metadataProvider.get("generalSeriesModule", imageId);
  var generalStudyModule = metadataProvider.get("generalStudyModule", imageId);
  var patientStudyModule = metadataProvider.get("patientStudyModule", imageId);
  var patientModule = metadataProvider.get("patientModule", imageId);
  var patientDemographicModule = metadataProvider.get("patientDemographicModule", imageId);
  return {
    Modality: generalSeriesModule.modality,
    PatientID: patientModule.patientId,
    PatientName: patientModule.patientName,
    PatientBirthDate: "",
    PatientAge: patientStudyModule.patientAge,
    PatientSex: patientDemographicModule.patientSex,
    PatientWeight: patientStudyModule.patientWeight,
    StudyDate: generalStudyModule.studyDate,
    StudyTime: generalStudyModule.studyTime,
    StudyID: "ToDo",
    AccessionNumber: generalStudyModule.accessionNumber
  };
}

function getReferencedFrameOfReferenceSequence(metadata, metadataProvider, dataset) {
  var imageId = metadata.referencedImageId,
    FrameOfReferenceUID = metadata.FrameOfReferenceUID;
  var instance = metadataProvider.get("instance", imageId);
  var SeriesInstanceUID = instance.SeriesInstanceUID;
  var ReferencedSeriesSequence = dataset.ReferencedSeriesSequence;
  return [{
    FrameOfReferenceUID: FrameOfReferenceUID,
    RTReferencedStudySequence: [{
      ReferencedSOPClassUID: dataset.SOPClassUID,
      ReferencedSOPInstanceUID: dataset.SOPInstanceUID,
      RTReferencedSeriesSequence: [{
        SeriesInstanceUID: SeriesInstanceUID,
        ContourImageSequence: _toConsumableArray(ReferencedSeriesSequence[0].ReferencedInstanceSequence)
      }]
    }]
  }];
}

function getReferencedSeriesSequence(metadata, _index, metadataProvider, DicomMetadataStore) {
  // grab imageId from toolData
  var imageId = metadata.referencedImageId;
  var instance = metadataProvider.get("instance", imageId);
  var SeriesInstanceUID = instance.SeriesInstanceUID,
    StudyInstanceUID = instance.StudyInstanceUID;
  var ReferencedSeriesSequence = [];
  if (SeriesInstanceUID) {
    var series = DicomMetadataStore.getSeries(StudyInstanceUID, SeriesInstanceUID);
    var ReferencedSeries = {
      SeriesInstanceUID: SeriesInstanceUID,
      ReferencedInstanceSequence: []
    };
    series.instances.forEach(function (instance) {
      var SOPInstanceUID = instance.SOPInstanceUID,
        SOPClassUID = instance.SOPClassUID;
      ReferencedSeries.ReferencedInstanceSequence.push({
        ReferencedSOPClassUID: SOPClassUID,
        ReferencedSOPInstanceUID: SOPInstanceUID
      });
    });
    ReferencedSeriesSequence.push(ReferencedSeries);
  }
  return ReferencedSeriesSequence;
}

function getRTROIObservationsSequence(toolData, index) {
  return {
    ObservationNumber: index + 1,
    ReferencedROINumber: index + 1,
    RTROIInterpretedType: "Todo: type",
    ROIInterpreter: "Todo: interpreter"
  };
}

function getRTSeriesModule(DicomMetaDictionary) {
  return {
    SeriesInstanceUID: DicomMetaDictionary.uid(),
    // generate a new series instance uid
    SeriesNumber: "99" // Todo:: what should be the series number?
  };
}

function getStructureSetModule(contour, index) {
  var FrameOfReferenceUID = contour.metadata.FrameOfReferenceUID;
  return {
    ROINumber: index + 1,
    ROIName: contour.name || "Todo: name ".concat(index + 1),
    ROIDescription: "Todo: description ".concat(index + 1),
    ROIGenerationAlgorithm: "Todo: algorithm",
    ReferencedFrameOfReferenceUID: FrameOfReferenceUID
  };
}

var DicomMetaDictionary = dcmjs__WEBPACK_IMPORTED_MODULE_0__["default"].data.DicomMetaDictionary;
/**
 * Convert handles to RTSS report containing the dcmjs dicom dataset.
 *
 * Note: current WIP and using segmentation to contour conversion,
 * routine that is not fully tested
 *
 * @param segmentations - Cornerstone tool segmentations data
 * @param metadataProvider - Metadata provider
 * @param DicomMetadataStore - metadata store instance
 * @param cs - cornerstone instance
 * @param csTools - cornerstone tool instance
 * @returns Report object containing the dataset
 */
function generateRTSSFromSegmentations(segmentations, metadataProvider, DicomMetadataStore, cornerstoneCache, cornerstoneToolsEnums, vtkUtils) {
    // Convert segmentations to ROIContours
    var roiContours = [];
    var contourSets = generateContourSetsFromLabelmap({
        segmentations: segmentations,
        cornerstoneCache: cornerstoneCache,
        cornerstoneToolsEnums: cornerstoneToolsEnums,
        vtkUtils: vtkUtils
    });
    contourSets.forEach(function (contourSet, segIndex) {
        // Check contour set isn't undefined
        if (contourSet) {
            var contourSequence_1 = [];
            contourSet.sliceContours.forEach(function (sliceContour) {
                /**
                 * addContour - Adds a new ROI with related contours to ROIContourSequence
                 *
                 * @param newContour - cornerstoneTools `ROIContour` object
                 *
                 * newContour = {
                 *   name: string,
                 *   description: string,
                 *   contourSequence: array[contour]
                 *   color: array[number],
                 *   metadata: {
                 *       referencedImageId: string,
                 *       FrameOfReferenceUID: string
                 *     }
                 * }
                 *
                 * contour = {
                 *   ContourImageSequence: array[
                 *       { ReferencedSOPClassUID: string, ReferencedSOPInstanceUID: string}
                 *     ]
                 *   ContourGeometricType: string,
                 *   NumberOfContourPoints: number,
                 *   ContourData: array[number]
                 * }
                 */
                // Note: change needed if support non-planar contour representation is needed
                var sopCommon = metadataProvider.get("sopCommonModule", sliceContour.referencedImageId);
                var ReferencedSOPClassUID = sopCommon.sopClassUID;
                var ReferencedSOPInstanceUID = sopCommon.sopInstanceUID;
                var ContourImageSequence = [
                    { ReferencedSOPClassUID: ReferencedSOPClassUID, ReferencedSOPInstanceUID: ReferencedSOPInstanceUID } // NOTE: replace in dcmjs?
                ];
                var sliceContourPolyData = sliceContour.polyData;
                sliceContour.contours.forEach(function (contour, index) {
                    var ContourGeometricType = contour.type;
                    var NumberOfContourPoints = contour.contourPoints.length;
                    var ContourData = [];
                    contour.contourPoints.forEach(function (point) {
                        var pointData = sliceContourPolyData.points[point];
                        pointData[0] = +pointData[0].toFixed(2);
                        pointData[1] = +pointData[1].toFixed(2);
                        pointData[2] = +pointData[2].toFixed(2);
                        ContourData.push(pointData[0]);
                        ContourData.push(pointData[1]);
                        ContourData.push(pointData[2]);
                    });
                    contourSequence_1.push({
                        ContourImageSequence: ContourImageSequence,
                        ContourGeometricType: ContourGeometricType,
                        NumberOfContourPoints: NumberOfContourPoints,
                        ContourNumber: index + 1,
                        ContourData: ContourData
                    });
                });
            });
            var segLabel = contourSet.label || "Segment ".concat(segIndex + 1);
            var ROIContour = {
                name: segLabel,
                description: segLabel,
                contourSequence: contourSequence_1,
                color: contourSet.color,
                metadata: contourSet.metadata
            };
            roiContours.push(ROIContour);
        }
    });
    var rtMetadata = {
        name: segmentations.label,
        label: segmentations.label
    };
    var dataset = _initializeDataset(rtMetadata, roiContours[0].metadata, metadataProvider);
    roiContours.forEach(function (contour, index) {
        var roiContour = {
            ROIDisplayColor: contour.color || [255, 0, 0],
            ContourSequence: contour.contourSequence,
            ReferencedROINumber: index + 1
        };
        dataset.StructureSetROISequence.push(getStructureSetModule(contour, index));
        dataset.ROIContourSequence.push(roiContour);
        // ReferencedSeriesSequence
        dataset.ReferencedSeriesSequence = getReferencedSeriesSequence(contour.metadata, index, metadataProvider, DicomMetadataStore);
        // ReferencedFrameOfReferenceSequence
        dataset.ReferencedFrameOfReferenceSequence =
            getReferencedFrameOfReferenceSequence(contour.metadata, metadataProvider, dataset);
    });
    var fileMetaInformationVersionArray = new Uint8Array(2);
    fileMetaInformationVersionArray[1] = 1;
    var _meta = {
        FileMetaInformationVersion: {
            Value: [fileMetaInformationVersionArray.buffer],
            vr: "OB"
        },
        TransferSyntaxUID: {
            Value: ["1.2.840.10008.1.2.1"],
            vr: "UI"
        },
        ImplementationClassUID: {
            Value: [DicomMetaDictionary.uid()],
            vr: "UI"
        },
        ImplementationVersionName: {
            Value: ["dcmjs"],
            vr: "SH"
        }
    };
    dataset._meta = _meta;
    return dataset;
}
/**
 * Convert handles to RTSSReport report object containing the dcmjs dicom dataset.
 *
 * Note: The tool data needs to be formatted in a specific way, and currently
 * it is limited to the RectangleROIStartEndTool in the Cornerstone.
 *
 * @param annotations Array of Cornerstone tool annotation data
 * @param metadataProvider Metadata provider
 * @param options report generation options
 * @returns Report object containing the dataset
 */
function generateRTSSFromAnnotations(annotations, metadataProvider, DicomMetadataStore, options) {
    var rtMetadata = {
        name: "RTSS from Annotations",
        label: "RTSS from Annotations"
    };
    var dataset = _initializeDataset(rtMetadata, annotations[0].metadata, metadataProvider);
    annotations.forEach(function (annotation, index) {
        var ContourSequence = AnnotationToPointData.convert(annotation, index, metadataProvider, options);
        dataset.StructureSetROISequence.push(getStructureSetModule(annotation, index));
        dataset.ROIContourSequence.push(ContourSequence);
        dataset.RTROIObservationsSequence.push(getRTROIObservationsSequence(annotation, index));
        // ReferencedSeriesSequence
        // Todo: handle more than one series
        dataset.ReferencedSeriesSequence = getReferencedSeriesSequence(annotation.metadata, index, metadataProvider, DicomMetadataStore);
        // ReferencedFrameOfReferenceSequence
        dataset.ReferencedFrameOfReferenceSequence =
            getReferencedFrameOfReferenceSequence(annotation.metadata, metadataProvider, dataset);
    });
    var fileMetaInformationVersionArray = new Uint8Array(2);
    fileMetaInformationVersionArray[1] = 1;
    var _meta = {
        FileMetaInformationVersion: {
            Value: [fileMetaInformationVersionArray.buffer],
            vr: "OB"
        },
        TransferSyntaxUID: {
            Value: ["1.2.840.10008.1.2.1"],
            vr: "UI"
        },
        ImplementationClassUID: {
            Value: [DicomMetaDictionary.uid()],
            vr: "UI"
        },
        ImplementationVersionName: {
            Value: ["dcmjs"],
            vr: "SH"
        }
    };
    dataset._meta = _meta;
    return dataset;
}
// /**
//  * Generate Cornerstone tool state from dataset
//  * @param {object} dataset dataset
//  * @param {object} hooks
//  * @param {function} hooks.getToolClass Function to map dataset to a tool class
//  * @returns
//  */
// //static generateToolState(_dataset, _hooks = {}) {
// function generateToolState() {
//     // Todo
//     console.warn("RTSS.generateToolState not implemented");
// }
function _initializeDataset(rtMetadata, imgMetadata, metadataProvider) {
    var rtSOPInstanceUID = DicomMetaDictionary.uid();
    // get the first annotation data
    var imageId = imgMetadata.referencedImageId, FrameOfReferenceUID = imgMetadata.FrameOfReferenceUID;
    var studyInstanceUID = metadataProvider.get("generalSeriesModule", imageId).studyInstanceUID;
    var patientModule = getPatientModule(imageId, metadataProvider);
    var rtSeriesModule = getRTSeriesModule(DicomMetaDictionary);
    return __assign(__assign(__assign({ StructureSetROISequence: [], ROIContourSequence: [], RTROIObservationsSequence: [], ReferencedSeriesSequence: [], ReferencedFrameOfReferenceSequence: [] }, patientModule), rtSeriesModule), { StudyInstanceUID: studyInstanceUID, SOPClassUID: "1.2.840.10008.5.1.4.1.1.481.3", SOPInstanceUID: rtSOPInstanceUID, Manufacturer: "dcmjs", Modality: "RTSTRUCT", FrameOfReferenceUID: FrameOfReferenceUID, PositionReferenceIndicator: "", StructureSetLabel: rtMetadata.label || "", StructureSetName: rtMetadata.name || "", ReferringPhysicianName: "", OperatorsName: "", StructureSetDate: DicomMetaDictionary.date(), StructureSetTime: DicomMetaDictionary.time() });
}

var RTSS = /*#__PURE__*/Object.freeze({
  __proto__: null,
  generateContourSetsFromLabelmap: generateContourSetsFromLabelmap,
  generateRTSSFromAnnotations: generateRTSSFromAnnotations,
  generateRTSSFromSegmentations: generateRTSSFromSegmentations
});

var Cornerstone3DSR = {
    Bidirectional: Bidirectional,
    CobbAngle: CobbAngle,
    Angle: Angle,
    Length: Length,
    CircleROI: CircleROI,
    EllipticalROI: EllipticalROI,
    RectangleROI: RectangleROI,
    ArrowAnnotate: ArrowAnnotate,
    Probe: Probe,
    PlanarFreehandROI: PlanarFreehandROI,
    MeasurementReport: MeasurementReport,
    CodeScheme: CodingScheme,
    CORNERSTONE_3D_TAG: CORNERSTONE_3D_TAG
};
var Cornerstone3DSEG = {
    Segmentation: Segmentation$1
};
var Cornerstone3DRT = {
    RTSS: RTSS
};

var Colors = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .data */ .aT.Colors,
  BitArray = dcmjs__WEBPACK_IMPORTED_MODULE_0__/* .data */ .aT.BitArray;

// TODO: Is there a better name for this? RGBAInt?
// Should we move it to Colors.js
function dicomlab2RGBA(cielab) {
  var rgba = Colors.dicomlab2RGB(cielab).map(function (x) {
    return Math.round(x * 255);
  });
  rgba.push(255);
  return rgba;
}

// TODO: Copied these functions in from VTK Math so we don't need a dependency.
// I guess we should put them somewhere
// https://github.com/Kitware/vtk-js/blob/master/Sources/Common/Core/Math/index.js
function cross(x, y, out) {
  var Zx = x[1] * y[2] - x[2] * y[1];
  var Zy = x[2] * y[0] - x[0] * y[2];
  var Zz = x[0] * y[1] - x[1] * y[0];
  out[0] = Zx;
  out[1] = Zy;
  out[2] = Zz;
}
function norm(x) {
  var n = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 3;
  switch (n) {
    case 1:
      return Math.abs(x);
    case 2:
      return Math.sqrt(x[0] * x[0] + x[1] * x[1]);
    case 3:
      return Math.sqrt(x[0] * x[0] + x[1] * x[1] + x[2] * x[2]);
    default:
      {
        var sum = 0;
        for (var i = 0; i < n; i++) {
          sum += x[i] * x[i];
        }
        return Math.sqrt(sum);
      }
  }
}
function normalize(x) {
  var den = norm(x);
  if (den !== 0.0) {
    x[0] /= den;
    x[1] /= den;
    x[2] /= den;
  }
  return den;
}
function subtract(a, b, out) {
  out[0] = a[0] - b[0];
  out[1] = a[1] - b[1];
  out[2] = a[2] - b[2];
}

// TODO: This is a useful utility on its own. We should move it somewhere?
// dcmjs.adapters.vtk.Multiframe? dcmjs.utils?
function geometryFromFunctionalGroups(dataset, PerFrameFunctionalGroups) {
  var geometry = {};
  var pixelMeasures = dataset.SharedFunctionalGroupsSequence.PixelMeasuresSequence;
  var planeOrientation = dataset.SharedFunctionalGroupsSequence.PlaneOrientationSequence;

  // Find the origin of the volume from the PerFrameFunctionalGroups' ImagePositionPatient values
  //
  // TODO: assumes sorted frames. This should read the ImagePositionPatient from each frame and
  // sort them to obtain the first and last position along the acquisition axis.
  var firstFunctionalGroup = PerFrameFunctionalGroups[0];
  var lastFunctionalGroup = PerFrameFunctionalGroups[PerFrameFunctionalGroups.length - 1];
  var firstPosition = firstFunctionalGroup.PlanePositionSequence.ImagePositionPatient.map(Number);
  var lastPosition = lastFunctionalGroup.PlanePositionSequence.ImagePositionPatient.map(Number);
  geometry.origin = firstPosition;

  // NB: DICOM PixelSpacing is defined as Row then Column,
  // unlike ImageOrientationPatient
  geometry.spacing = [pixelMeasures.PixelSpacing[1], pixelMeasures.PixelSpacing[0], pixelMeasures.SpacingBetweenSlices].map(Number);
  geometry.dimensions = [dataset.Columns, dataset.Rows, PerFrameFunctionalGroups.length].map(Number);
  var orientation = planeOrientation.ImageOrientationPatient.map(Number);
  var columnStepToPatient = orientation.slice(0, 3);
  var rowStepToPatient = orientation.slice(3, 6);
  geometry.planeNormal = [];
  cross(columnStepToPatient, rowStepToPatient, geometry.planeNormal);
  geometry.sliceStep = [];
  subtract(lastPosition, firstPosition, geometry.sliceStep);
  normalize(geometry.sliceStep);
  geometry.direction = columnStepToPatient.concat(rowStepToPatient).concat(geometry.sliceStep);
  return geometry;
}
var Segmentation = /*#__PURE__*/function () {
  function Segmentation() {
    _classCallCheck(this, Segmentation);
  }

  /**
   * Produces an array of Segments from an input DICOM Segmentation dataset
   *
   * Segments are returned with Geometry values that can be used to create
   * VTK Image Data objects.
   *
   * @example Example usage to create VTK Volume actors from each segment:
   *
   * const actors = [];
   * const segments = generateToolState(dataset);
   * segments.forEach(segment => {
   *   // now make actors using the segment information
   *   const scalarArray = vtk.Common.Core.vtkDataArray.newInstance({
   *        name: "Scalars",
   *        numberOfComponents: 1,
   *        values: segment.pixelData,
   *    });
   *
   *    const imageData = vtk.Common.DataModel.vtkImageData.newInstance();
   *    imageData.getPointData().setScalars(scalarArray);
   *    imageData.setDimensions(geometry.dimensions);
   *    imageData.setSpacing(geometry.spacing);
   *    imageData.setOrigin(geometry.origin);
   *    imageData.setDirection(geometry.direction);
   *
   *    const mapper = vtk.Rendering.Core.vtkVolumeMapper.newInstance();
   *    mapper.setInputData(imageData);
   *    mapper.setSampleDistance(2.);
   *
   *    const actor = vtk.Rendering.Core.vtkVolume.newInstance();
   *    actor.setMapper(mapper);
   *
   *    actors.push(actor);
   * });
   *
   * @param dataset
   * @return {{}}
   */
  _createClass(Segmentation, null, [{
    key: "generateSegments",
    value: function generateSegments(dataset) {
      if (dataset.SegmentSequence.constructor.name !== "Array") {
        dataset.SegmentSequence = [dataset.SegmentSequence];
      }
      dataset.SegmentSequence.forEach(function (segment) {
        // TODO: other interesting fields could be extracted from the segment
        // TODO: Read SegmentsOverlay field
        // http://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.8.20.2.html

        // TODO: Looks like vtkColor only wants RGB in 0-1 values.
        // Why was this example converting to RGBA with 0-255 values?
        var color = dicomlab2RGBA(segment.RecommendedDisplayCIELabValue);
        segments[segment.SegmentNumber] = {
          color: color,
          functionalGroups: [],
          offset: null,
          size: null,
          pixelData: null
        };
      });

      // make a list of functional groups per segment
      dataset.PerFrameFunctionalGroupsSequence.forEach(function (functionalGroup) {
        var segmentNumber = functionalGroup.SegmentIdentificationSequence.ReferencedSegmentNumber;
        segments[segmentNumber].functionalGroups.push(functionalGroup);
      });

      // determine per-segment index into the pixel data
      // TODO: only handles one-bit-per pixel
      var frameSize = Math.ceil(dataset.Rows * dataset.Columns / 8);
      var nextOffset = 0;
      Object.keys(segments).forEach(function (segmentNumber) {
        var segment = segments[segmentNumber];
        segment.numberOfFrames = segment.functionalGroups.length;
        segment.size = segment.numberOfFrames * frameSize;
        segment.offset = nextOffset;
        nextOffset = segment.offset + segment.size;
        var packedSegment = dataset.PixelData.slice(segment.offset, nextOffset);
        segment.pixelData = BitArray.unpack(packedSegment);
        var geometry = geometryFromFunctionalGroups(dataset, segment.functionalGroups);
        segment.geometry = geometry;
      });
      return segments;
    }
  }]);
  return Segmentation;
}();

var VTKjsSEG = {
    Segmentation: Segmentation
};

var adaptersSR = {
    Cornerstone: CornerstoneSR,
    Cornerstone3D: Cornerstone3DSR
};
var adaptersSEG = {
    Cornerstone: CornerstoneSEG,
    Cornerstone3D: Cornerstone3DSEG,
    VTKjs: VTKjsSEG
};
var adaptersRT = {
    Cornerstone3D: Cornerstone3DRT
};




/***/ }),

/***/ 27318:
/***/ ((module) => {

"use strict";


function iota(n) {
  var result = new Array(n)
  for(var i=0; i<n; ++i) {
    result[i] = i
  }
  return result
}

module.exports = iota

/***/ }),

/***/ 8516:
/***/ ((module) => {

/*!
 * Determine if an object is a Buffer
 *
 * @author   Feross Aboukhadijeh <https://feross.org>
 * @license  MIT
 */

module.exports = function isBuffer (obj) {
  return obj != null && obj.constructor != null &&
    typeof obj.constructor.isBuffer === 'function' && obj.constructor.isBuffer(obj)
}


/***/ }),

/***/ 87513:
/***/ ((module, __unused_webpack_exports, __webpack_require__) => {

var iota = __webpack_require__(27318)
var isBuffer = __webpack_require__(8516)

var hasTypedArrays  = ((typeof Float64Array) !== "undefined")

function compare1st(a, b) {
  return a[0] - b[0]
}

function order() {
  var stride = this.stride
  var terms = new Array(stride.length)
  var i
  for(i=0; i<terms.length; ++i) {
    terms[i] = [Math.abs(stride[i]), i]
  }
  terms.sort(compare1st)
  var result = new Array(terms.length)
  for(i=0; i<result.length; ++i) {
    result[i] = terms[i][1]
  }
  return result
}

function compileConstructor(dtype, dimension) {
  var className = ["View", dimension, "d", dtype].join("")
  if(dimension < 0) {
    className = "View_Nil" + dtype
  }
  var useGetters = (dtype === "generic")

  if(dimension === -1) {
    //Special case for trivial arrays
    var code =
      "function "+className+"(a){this.data=a;};\
var proto="+className+".prototype;\
proto.dtype='"+dtype+"';\
proto.index=function(){return -1};\
proto.size=0;\
proto.dimension=-1;\
proto.shape=proto.stride=proto.order=[];\
proto.lo=proto.hi=proto.transpose=proto.step=\
function(){return new "+className+"(this.data);};\
proto.get=proto.set=function(){};\
proto.pick=function(){return null};\
return function construct_"+className+"(a){return new "+className+"(a);}"
    var procedure = new Function(code)
    return procedure()
  } else if(dimension === 0) {
    //Special case for 0d arrays
    var code =
      "function "+className+"(a,d) {\
this.data = a;\
this.offset = d\
};\
var proto="+className+".prototype;\
proto.dtype='"+dtype+"';\
proto.index=function(){return this.offset};\
proto.dimension=0;\
proto.size=1;\
proto.shape=\
proto.stride=\
proto.order=[];\
proto.lo=\
proto.hi=\
proto.transpose=\
proto.step=function "+className+"_copy() {\
return new "+className+"(this.data,this.offset)\
};\
proto.pick=function "+className+"_pick(){\
return TrivialArray(this.data);\
};\
proto.valueOf=proto.get=function "+className+"_get(){\
return "+(useGetters ? "this.data.get(this.offset)" : "this.data[this.offset]")+
"};\
proto.set=function "+className+"_set(v){\
return "+(useGetters ? "this.data.set(this.offset,v)" : "this.data[this.offset]=v")+"\
};\
return function construct_"+className+"(a,b,c,d){return new "+className+"(a,d)}"
    var procedure = new Function("TrivialArray", code)
    return procedure(CACHED_CONSTRUCTORS[dtype][0])
  }

  var code = ["'use strict'"]

  //Create constructor for view
  var indices = iota(dimension)
  var args = indices.map(function(i) { return "i"+i })
  var index_str = "this.offset+" + indices.map(function(i) {
        return "this.stride[" + i + "]*i" + i
      }).join("+")
  var shapeArg = indices.map(function(i) {
      return "b"+i
    }).join(",")
  var strideArg = indices.map(function(i) {
      return "c"+i
    }).join(",")
  code.push(
    "function "+className+"(a," + shapeArg + "," + strideArg + ",d){this.data=a",
      "this.shape=[" + shapeArg + "]",
      "this.stride=[" + strideArg + "]",
      "this.offset=d|0}",
    "var proto="+className+".prototype",
    "proto.dtype='"+dtype+"'",
    "proto.dimension="+dimension)

  //view.size:
  code.push("Object.defineProperty(proto,'size',{get:function "+className+"_size(){\
return "+indices.map(function(i) { return "this.shape["+i+"]" }).join("*"),
"}})")

  //view.order:
  if(dimension === 1) {
    code.push("proto.order=[0]")
  } else {
    code.push("Object.defineProperty(proto,'order',{get:")
    if(dimension < 4) {
      code.push("function "+className+"_order(){")
      if(dimension === 2) {
        code.push("return (Math.abs(this.stride[0])>Math.abs(this.stride[1]))?[1,0]:[0,1]}})")
      } else if(dimension === 3) {
        code.push(
"var s0=Math.abs(this.stride[0]),s1=Math.abs(this.stride[1]),s2=Math.abs(this.stride[2]);\
if(s0>s1){\
if(s1>s2){\
return [2,1,0];\
}else if(s0>s2){\
return [1,2,0];\
}else{\
return [1,0,2];\
}\
}else if(s0>s2){\
return [2,0,1];\
}else if(s2>s1){\
return [0,1,2];\
}else{\
return [0,2,1];\
}}})")
      }
    } else {
      code.push("ORDER})")
    }
  }

  //view.set(i0, ..., v):
  code.push(
"proto.set=function "+className+"_set("+args.join(",")+",v){")
  if(useGetters) {
    code.push("return this.data.set("+index_str+",v)}")
  } else {
    code.push("return this.data["+index_str+"]=v}")
  }

  //view.get(i0, ...):
  code.push("proto.get=function "+className+"_get("+args.join(",")+"){")
  if(useGetters) {
    code.push("return this.data.get("+index_str+")}")
  } else {
    code.push("return this.data["+index_str+"]}")
  }

  //view.index:
  code.push(
    "proto.index=function "+className+"_index(", args.join(), "){return "+index_str+"}")

  //view.hi():
  code.push("proto.hi=function "+className+"_hi("+args.join(",")+"){return new "+className+"(this.data,"+
    indices.map(function(i) {
      return ["(typeof i",i,"!=='number'||i",i,"<0)?this.shape[", i, "]:i", i,"|0"].join("")
    }).join(",")+","+
    indices.map(function(i) {
      return "this.stride["+i + "]"
    }).join(",")+",this.offset)}")

  //view.lo():
  var a_vars = indices.map(function(i) { return "a"+i+"=this.shape["+i+"]" })
  var c_vars = indices.map(function(i) { return "c"+i+"=this.stride["+i+"]" })
  code.push("proto.lo=function "+className+"_lo("+args.join(",")+"){var b=this.offset,d=0,"+a_vars.join(",")+","+c_vars.join(","))
  for(var i=0; i<dimension; ++i) {
    code.push(
"if(typeof i"+i+"==='number'&&i"+i+">=0){\
d=i"+i+"|0;\
b+=c"+i+"*d;\
a"+i+"-=d}")
  }
  code.push("return new "+className+"(this.data,"+
    indices.map(function(i) {
      return "a"+i
    }).join(",")+","+
    indices.map(function(i) {
      return "c"+i
    }).join(",")+",b)}")

  //view.step():
  code.push("proto.step=function "+className+"_step("+args.join(",")+"){var "+
    indices.map(function(i) {
      return "a"+i+"=this.shape["+i+"]"
    }).join(",")+","+
    indices.map(function(i) {
      return "b"+i+"=this.stride["+i+"]"
    }).join(",")+",c=this.offset,d=0,ceil=Math.ceil")
  for(var i=0; i<dimension; ++i) {
    code.push(
"if(typeof i"+i+"==='number'){\
d=i"+i+"|0;\
if(d<0){\
c+=b"+i+"*(a"+i+"-1);\
a"+i+"=ceil(-a"+i+"/d)\
}else{\
a"+i+"=ceil(a"+i+"/d)\
}\
b"+i+"*=d\
}")
  }
  code.push("return new "+className+"(this.data,"+
    indices.map(function(i) {
      return "a" + i
    }).join(",")+","+
    indices.map(function(i) {
      return "b" + i
    }).join(",")+",c)}")

  //view.transpose():
  var tShape = new Array(dimension)
  var tStride = new Array(dimension)
  for(var i=0; i<dimension; ++i) {
    tShape[i] = "a[i"+i+"]"
    tStride[i] = "b[i"+i+"]"
  }
  code.push("proto.transpose=function "+className+"_transpose("+args+"){"+
    args.map(function(n,idx) { return n + "=(" + n + "===undefined?" + idx + ":" + n + "|0)"}).join(";"),
    "var a=this.shape,b=this.stride;return new "+className+"(this.data,"+tShape.join(",")+","+tStride.join(",")+",this.offset)}")

  //view.pick():
  code.push("proto.pick=function "+className+"_pick("+args+"){var a=[],b=[],c=this.offset")
  for(var i=0; i<dimension; ++i) {
    code.push("if(typeof i"+i+"==='number'&&i"+i+">=0){c=(c+this.stride["+i+"]*i"+i+")|0}else{a.push(this.shape["+i+"]);b.push(this.stride["+i+"])}")
  }
  code.push("var ctor=CTOR_LIST[a.length+1];return ctor(this.data,a,b,c)}")

  //Add return statement
  code.push("return function construct_"+className+"(data,shape,stride,offset){return new "+className+"(data,"+
    indices.map(function(i) {
      return "shape["+i+"]"
    }).join(",")+","+
    indices.map(function(i) {
      return "stride["+i+"]"
    }).join(",")+",offset)}")

  //Compile procedure
  var procedure = new Function("CTOR_LIST", "ORDER", code.join("\n"))
  return procedure(CACHED_CONSTRUCTORS[dtype], order)
}

function arrayDType(data) {
  if(isBuffer(data)) {
    return "buffer"
  }
  if(hasTypedArrays) {
    switch(Object.prototype.toString.call(data)) {
      case "[object Float64Array]":
        return "float64"
      case "[object Float32Array]":
        return "float32"
      case "[object Int8Array]":
        return "int8"
      case "[object Int16Array]":
        return "int16"
      case "[object Int32Array]":
        return "int32"
      case "[object Uint8Array]":
        return "uint8"
      case "[object Uint16Array]":
        return "uint16"
      case "[object Uint32Array]":
        return "uint32"
      case "[object Uint8ClampedArray]":
        return "uint8_clamped"
      case "[object BigInt64Array]":
        return "bigint64"
      case "[object BigUint64Array]":
        return "biguint64"
    }
  }
  if(Array.isArray(data)) {
    return "array"
  }
  return "generic"
}

var CACHED_CONSTRUCTORS = {
  "float32":[],
  "float64":[],
  "int8":[],
  "int16":[],
  "int32":[],
  "uint8":[],
  "uint16":[],
  "uint32":[],
  "array":[],
  "uint8_clamped":[],
  "bigint64": [],
  "biguint64": [],
  "buffer":[],
  "generic":[]
}

;(function() {
  for(var id in CACHED_CONSTRUCTORS) {
    CACHED_CONSTRUCTORS[id].push(compileConstructor(id, -1))
  }
});

function wrappedNDArrayCtor(data, shape, stride, offset) {
  if(data === undefined) {
    var ctor = CACHED_CONSTRUCTORS.array[0]
    return ctor([])
  } else if(typeof data === "number") {
    data = [data]
  }
  if(shape === undefined) {
    shape = [ data.length ]
  }
  var d = shape.length
  if(stride === undefined) {
    stride = new Array(d)
    for(var i=d-1, sz=1; i>=0; --i) {
      stride[i] = sz
      sz *= shape[i]
    }
  }
  if(offset === undefined) {
    offset = 0
    for(var i=0; i<d; ++i) {
      if(stride[i] < 0) {
        offset -= (shape[i]-1)*stride[i]
      }
    }
  }
  var dtype = arrayDType(data)
  var ctor_list = CACHED_CONSTRUCTORS[dtype]
  while(ctor_list.length <= d+1) {
    ctor_list.push(compileConstructor(dtype, ctor_list.length-1))
  }
  var ctor = ctor_list[d+1]
  return ctor(data, shape, stride, offset)
}

module.exports = wrappedNDArrayCtor


/***/ })

}]);