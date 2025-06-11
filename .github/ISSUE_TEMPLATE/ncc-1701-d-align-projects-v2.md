---
title: [NCC-1701-D] Align Projects v2 Fields and Automation with Org Standards
labels: [github-projects, v2, automation, copilot, cross-repo]
assignees: []
---

## 🎯 Objective
Update the NCC-1701-D repository and its associated GitHub Projects v2 to use the org-standard field prompts and validation for all automation and workflows.

## 📋 Description
The dynamicstms365 org is standardizing all Projects v2 fields and values using YAML prompts in `.github/prompts/`. This repo should:
- Reference and use these prompts for all project automation
- Update documentation and workflows to require prompt-based validation
- Ensure all new project items use only approved values
- Communicate with other repos to ensure cross-repo orchestration uses the same standards

## 🔧 Actions Required
- [ ] Audit all Projects v2 in this repo for field alignment
- [ ] Update fields/options to match `.github/prompts/` YAMLs
- [ ] Update automation and documentation to require prompt-based validation
- [ ] Communicate changes to contributors and cross-repo maintainers

## ✅ Acceptance Criteria
- [ ] All repo projects have the same set of custom fields/options as org standard
- [ ] All automation uses prompt-based validation
- [ ] No API/CLI failures due to invalid field values
- [ ] Contributors and cross-repo maintainers are aware of the new process

## 🔗 Dependencies
- `.github/prompts/` YAMLs
- gh copilot CLI or compatible extensions

## 📊 Success Metrics
- 100% field alignment across repo projects
- 0 API/CLI failures due to field value issues
- All new projects use the canonical field set
