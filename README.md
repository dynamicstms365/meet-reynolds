lets try this differently.  use these as your primary sources

https://docs.github.com/en/enterprise-cloud@latest/copilot/customizing-copilot/extending-the-capabilities-of-github-copilot-in-your-organization

https://docs.github.com/en/enterprise-cloud@latest/copilot/customizing-copilot/customizing-the-development-environment-for-copilot-coding-agent

https://docs.github.com/en/enterprise-cloud@latest/copilot/building-copilot-extensions/about-building-copilot-extensions

https://docs.github.com/en/enterprise-cloud@latest/copilot/building-copilot-extensions/building-a-copilot-agent-for-your-copilot-extension/about-copilot-agents

https://docs.github.com/en/enterprise-cloud@latest/copilot/building-copilot-extensions/building-a-copilot-skillset-for-your-copilot-extension/about-copilot-skillsets

https://pnp.github.io/cli-microsoft365/cmd/docs

https://pnp.github.io/cli-microsoft365/sample-scripts/introduction

https://learn.microsoft.com/en-us/power-platform/developer/cli/reference/

you should create markdown files that organize these contents in such a way that it can be easily consumed and understood by agents and llms.  A big part of the pac cli is creating environments and making sure proper apps are installed

## 🧑‍💻 Organizational Interaction Profiles

A new automation script is available to generate living, self-updating GitHub org user profiles:

- **Script:** `scripts/setup/generate-org-profiles.sh`
- **Output:** Markdown profiles in `docs/org-profiles/`
- **Purpose:** Analyze org members, activity, and generate context-aware documentation for collaboration and onboarding.

### Usage
```bash
./scripts/setup/generate-org-profiles.sh <github-org>
```

See `docs/org-profiles/README.md` for details.

---

*This feature supports the self-improving, knowledge-accumulating AI ecosystem. Profiles are designed for future integration with Teams and advanced analytics.*

## 🧑‍💻 Org Profile Analytics & Teams Integration (Planned)

- **Advanced Analytics:** The org profile script will be enhanced to extract collaboration networks, review patterns, and expertise heatmaps, and visualize them in a dashboard (`docs/org-profiles/dashboard.md`).
- **Teams Integration:** Roadmap includes mapping GitHub users to Teams users, extracting communication patterns, and cross-referencing collaboration data for a unified view.
- **Automation:** All analytics and dashboards will be auto-updating, supporting the self-improving AI ecosystem vision.

See `docs/org-profiles/README.md` for details and progress.
