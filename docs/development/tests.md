# Data Validation

Because DICOM is a historical standard with a long history of implementations that conform to the standard to varying degrees,
we expect DICOM data to vary widely in its strict adherence to the standard.

**Our general approach is to be has lenient as possible, accepting DICOM data unless it has a direct effect on functionality.**

This approach plays out in the following ways currently:
1. When DICOM data is received via a STOW request, we only validate data attributes that are indexed by default or via extended query tag.
2. We will attempt to store all other data attributes as they are. 
3. When new data attributes are indexed, extended query tag API handles errors gracefully, by continuing on validation errors and
getting explicit consent to allow searching partially indexed data.
4. New functionality should account for the presence of invalid data in unindexed attributes.

Data validation errors are communicated on each request by response status codes and failure codes documented in the conformance statement.
