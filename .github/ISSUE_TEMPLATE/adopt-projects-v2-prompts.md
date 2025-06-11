---
title: Adopt Projects v2 Field Standardization and Prompt Validation
labels: [github-projects, v2, automation, copilot]
assignees: []
---

## 🎯 Objective
Update this repository to use the org-standard Projects v2 field prompts and validation for all automation and workflows.

## 📋 Description
The dynamicstms365 org is standardizing all Projects v2 fields and values using YAML prompts in `.github/prompts/`. This repo should:
- Reference and use these prompts for all project automation
- Update documentation and workflows to require prompt-based validation
- Ensure all new project items use only approved values

## 🔧 Actions Required
- [ ] Update automation to use `.github/prompts/` YAMLs for field validation
- [ ] Update documentation to reference prompt usage
- [ ] Communicate changes to contributors

## ✅ Acceptance Criteria
- [ ] All project automation uses prompt-based validation
- [ ] No API/CLI failures due to invalid field values
- [ ] Contributors are aware of the new process

## 🔗 Dependencies
- `.github/prompts/` YAMLs
- gh copilot CLI or compatible extensions

## 📊 Success Metrics
- 100% prompt-based validation in automation
- 0 API/CLI failures due to field value issues
