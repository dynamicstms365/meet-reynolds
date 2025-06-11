---
title: Standardize GitHub Projects v2 Fields Across Organization
labels: [automation, github-projects, v2, standardization]
assignees: []
---

## 🎯 Objective
Standardize all GitHub Projects v2 in the dynamicstms365 organization to use the canonical set of custom fields and options as defined in `.github/prompts/`.

## 📋 Description
Several Projects v2 in the org have different custom fields and options, leading to automation/API failures and inconsistent workflows. This issue proposes:
- Auditing all org projects for field alignment
- Updating fields/options to match the canonical set (Status, Priority, Size, Iteration, Estimate, Dates, etc.)
- Enforcing prompt-based validation in all automation and documentation

## 🔧 Actions Required
- [ ] Audit all org Projects v2 for field alignment
- [ ] Update fields/options to match `.github/prompts/` YAMLs
- [ ] Update automation and documentation to require prompt-based validation
- [ ] Communicate changes to all repo maintainers

## ✅ Acceptance Criteria
- [ ] All org projects have the same set of custom fields/options
- [ ] All automation uses prompt-based validation
- [ ] No API/CLI failures due to invalid field values

## 🔗 Dependencies
- `.github/prompts/` YAMLs
- gh copilot CLI or compatible extensions

## 📊 Success Metrics
- 100% field alignment across org projects
- 0 API/CLI failures due to field value issues
- All new projects use the canonical field set
