# Tests

![Testing pyramid](/docs/images/TestPyramid.png)

- More unit tests, less integration tests, even lesser EnE tests.
- Unit tests are used to test all combination of valid and invalid cases for a specific component.
- E2E tests to validate all the integrations works for p0 scenario.
- Manual directed bug bashes are used to augment the automated testing.

## Unit tests
<em>Smallest testable part of software. </em>
- All business logic pinned with unit tests.
 
## Integration tests
<em>Individual units are combined and tested as a group.</em>
- DICOM uses integration tests to test the persistence layer.

## End to End (E2E) tests
<em>End to end user scenarios are tested.</em>
- DICOM uses E2E test methodology to test Web API endpoint behaviors.
