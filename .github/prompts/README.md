# GitHub Projects v2 Prompts

This directory contains YAML prompt files for each custom field used in GitHub Projects v2 across the organization. These prompts provide strict, org-approved values and validation rules to prevent API/CLI failures and ensure consistency.

## Usage
- All automation, CLI, and Copilot/model-based workflows **must** use these prompts for field value validation.
- If a value is not present in the prompt, update the prompt and the project configuration before proceeding.
- Use the `gh copilot` CLI or compatible models/extensions to enforce prompt-based validation.

## Available Prompts
- `status.yml`: Valid values for Status field
- `priority.yml`: Valid values for Priority field
- `size.yml`: Valid values for Size field
- `iteration.yml`: Valid values for Iteration field
- `estimate.yml`: Valid values for Estimate field
- `date.yml`: Valid date format for Start/End Date fields
- `labels.yml`: Valid labels (must exist in repo/org)
- `milestone.yml`: Valid milestones (must exist in repo/org)
- `repository.yml`: Valid repositories (must exist in org)
- `reviewers.yml`: Valid reviewers (must exist in repo/org)
- `parent_issue.yml`: Valid parent issues (must exist in repo/org)
- `sub_issues_progress.yml`: Valid sub-issues progress values
- `assignees.yml`: Valid assignees (must exist in repo/org)
- `linked_pull_requests.yml`: Valid pull requests (must exist in repo/org)
- `title.yml`: Title field guidance

## Extending Prompts
- When adding new fields or options, create or update the corresponding prompt YAML.
- Ensure all projects and automation are updated to use the new/updated prompt.

## Example
```yaml
name: status
values:
  - Todo/New
  - Blocked
  - In Progress/Active
  - Testing/Review
  - Done/Closed
strict: true
```

## Enforcement
- All Copilot agents, CLI, and automation must enforce prompt-based validation before making API/CLI calls to Projects v2.
- If a value is not present, update the prompt and project configuration before proceeding.
