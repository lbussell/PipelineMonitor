# Azure Pipelines Examples

This directory contains example Azure Pipelines YAML files demonstrating various parameter configurations.

## all-parameter-types.yml

This example demonstrates **all supported Azure Pipelines parameter types** with complete coverage of:

### Parameter Types Covered

1. **String** - Text values
   - With default value
   - Without default value
   - With allowed values (enumeration)

2. **StringList** - Array of strings
   - With default value
   - Without default value

3. **Number** - Numeric values
   - With default value
   - Without default value

4. **Boolean** - True/false values
   - With default value
   - Without default value

5. **Object** - Complex objects with nested properties
   - With default value
   - Without default value

6. **Step** - Single pipeline step
   - With default value
   - Without default value

7. **StepList** - Array of pipeline steps
   - With default value
   - Without default value

8. **Job** - Single pipeline job
   - With default value
   - Without default value

9. **JobList** - Array of pipeline jobs
   - With default value
   - Without default value

10. **Deployment** - Single deployment job
    - With default value
    - Without default value

11. **DeploymentList** - Array of deployment jobs
    - With default value
    - Without default value

12. **Stage** - Single pipeline stage
    - With default value
    - Without default value

13. **StageList** - Array of pipeline stages
    - With default value
    - Without default value

### Purpose

This example serves as:
- **Reference documentation** for all supported parameter types
- **Test data** for validating the PipelineMonitor parameter parsing functionality
- **Template** for developers creating parameterized Azure Pipelines

### Total Parameters

The file contains **27 parameters** covering all 13 parameter types, each with at least:
- One parameter with a default value
- One parameter without a default value

Additionally, string parameters demonstrate the use of `displayName` and `values` properties.
