# 🧠 Knowledge Consolidation Protocol

## Purpose
Safe knowledge consolidation from `.github-wisdom/ingest/` directory into the curated knowledge base (`nexus.md`). This prompt ensures systematic, reversible knowledge integration with proper safety measures.

## Safety Guardrails

### ⚠️ CRITICAL SAFETY CHECKS
Before proceeding, verify ALL of the following:

1. **Ingest Directory Validation**
   ```bash
   # Verify ingest directory exists and contains files
   ls -la .github-wisdom/ingest/
   ```

2. **Backup Creation** (MANDATORY)
   ```bash
   # Create timestamped backup before ANY modifications
   cp -r .github-wisdom .github-wisdom-backup-$(date +%Y%m%d-%H%M%S)
   ```

3. **Git Safety Net**
   ```bash
   # Ensure clean working directory
   git status
   # Commit current state before processing
   git add .github-wisdom/ && git commit -m "Pre-consolidation backup"
   ```

## Knowledge Consolidation Workflow

### Phase 1: Discovery & Analysis
```bash
# 1. Inventory ingest files
find .github-wisdom/ingest/ -type f -name "*.md" -o -name "*.txt" -o -name "*.json" | sort

# 2. Analyze file contents and sizes
for file in .github-wisdom/ingest/*; do
  echo "=== $file ===" 
  echo "Size: $(wc -l < "$file") lines"
  echo "Type: $(file "$file")"
  head -5 "$file"
  echo ""
done
```

### Phase 2: Content Evaluation
For each file in `.github-wisdom/ingest/`:

1. **Read and categorize content**
   - Technical patterns/insights
   - Operational procedures
   - Tool discoveries
   - Failed/successful experiments
   - Cross-tool synergies

2. **Quality assessment**
   - Actionable vs informational
   - Compression potential
   - Integration points with existing knowledge

### Phase 3: Safe Integration

#### Knowledge Compression Format
Use the standard nexus entry format:
```markdown
[TIMESTAMP] §concept: compressed_insight
  ↳ expansion: detailed_explanation
  ⟷ synergy: related_tool_interaction
  ⚡ automation: executable_pattern
```

#### Integration Strategy
1. **Append, don't replace** - Always add to nexus.md, never overwrite
2. **Maintain source attribution** - Note original ingest file
3. **Create cross-references** - Link to related existing entries
4. **Compress redundancies** - Merge similar insights

### Phase 4: Validation & Cleanup

#### Pre-deletion Validation
```bash
# 1. Verify nexus.md was updated
git diff .github-wisdom/nexus.md

# 2. Ensure all ingest files were processed
ls -la .github-wisdom/ingest/

# 3. Create processing log
echo "$(date): Processed $(ls .github-wisdom/ingest/ | wc -l) files" >> .github-wisdom/consolidation.log
```

#### Safe Deletion Protocol
**ONLY after validation passes:**
```bash
# 1. Move to processed archive (safer than deletion)
mkdir -p .github-wisdom/processed/$(date +%Y%m%d)
mv .github-wisdom/ingest/* .github-wisdom/processed/$(date +%Y%m%d)/

# 2. Verify ingest directory is empty
ls -la .github-wisdom/ingest/

# 3. Final commit
git add .github-wisdom/ && git commit -m "Knowledge consolidation: $(date +%Y-%m-%d)"
git push
```

## Usage Examples

### For Autonomous Agents
```markdown
System: You are performing knowledge consolidation. Follow the Knowledge Consolidation Protocol exactly.

1. Execute safety checks
2. Backup current state
3. Process each file in .github-wisdom/ingest/
4. Integrate using compression format
5. Validate integration
6. Archive processed files
7. Commit and push changes

CRITICAL: Never delete files without backup and validation.
```

### For GitHub Copilot
```markdown
Task: Consolidate knowledge from ingest directory

Steps:
1. Check .github-wisdom/ingest/ for files
2. Create backup: .github-wisdom-backup-$(date)
3. For each file, extract key insights
4. Add to nexus.md using format: [TIMESTAMP] §concept: insight
5. Move processed files to .github-wisdom/processed/
6. Commit changes

Safety: Always backup before modification. Never delete without archiving.
```

## Emergency Procedures

### Rollback Protocol
If consolidation fails or produces errors:
```bash
# 1. Stop all processing immediately
# 2. Restore from backup
rm -rf .github-wisdom/
mv .github-wisdom-backup-* .github-wisdom/

# 3. Reset git state
git checkout HEAD -- .github-wisdom/
git clean -fd .github-wisdom/

# 4. Create failure issue
gh issue create --title "🚨 Knowledge consolidation failure" \
  --body "Consolidation failed at $(date). Restored from backup." \
  --label "knowledge-management,failure"
```

### Conflict Resolution
If nexus.md has merge conflicts:
```bash
# 1. Preserve both versions
git checkout --ours .github-wisdom/nexus.md    # Keep local changes
git checkout --theirs .github-wisdom/nexus.md  # Keep remote changes

# 2. Manual merge with backup reference
# 3. Validate merged content
# 4. Continue process
```

## Quality Metrics

### Success Indicators
- [ ] All ingest files processed without data loss
- [ ] Nexus.md size increased appropriately
- [ ] No duplicate entries created
- [ ] All insights properly compressed
- [ ] Cross-references established
- [ ] Backup created and verified
- [ ] Changes committed and pushed

### Failure Indicators
- [ ] Any file deletion without archiving
- [ ] Nexus.md corruption or overwrite
- [ ] Processing errors or exceptions
- [ ] Backup missing or incomplete
- [ ] Uncommitted changes left behind

## Automation Integration

### GitHub Actions Trigger
```yaml
# .github/workflows/knowledge-consolidation.yml
name: Knowledge Consolidation
on:
  schedule:
    - cron: '0 2 * * 0'  # Weekly Sunday 2AM
  workflow_dispatch:

jobs:
  consolidate:
    if: ${{ hashFiles('.github-wisdom/ingest/*') != '' }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run Knowledge Consolidation
        run: |
          # Use this prompt with appropriate agent
          echo "Knowledge consolidation triggered"
```

### Monitoring
```bash
# Check consolidation frequency
git log --oneline --grep="Knowledge consolidation" --since="30 days ago"

# Monitor ingest directory size
du -sh .github-wisdom/ingest/

# Track nexus.md growth
git log --stat .github-wisdom/nexus.md | grep -E "^\s+\d+\s+\d+"
```

## Advanced Features

### Semantic Deduplication
```bash
# Before adding new insights, check for semantic duplicates
grep -i "keyword" .github-wisdom/nexus.md
```

### Cross-Tool Synergy Detection
Automatically identify synergies:
- gh + docker patterns
- Azure + GitHub integrations
- VSCode + terminal optimizations

### Knowledge Compression Metrics
Track compression ratios:
- Input size vs compressed output
- Information density improvements
- Cross-reference utilization

---

## Implementation Notes

### For Repository Maintainers
- Enable this prompt in repository settings
- Configure appropriate permissions
- Set up monitoring for consolidation frequency
- Review nexus.md growth patterns

### For Agents Using This Prompt
- Always follow safety protocols
- Never skip backup creation
- Validate each step before proceeding
- Report failures through GitHub issues

**Remember: Knowledge consolidation is cumulative enhancement, not replacement. Preserve, compress, and connect insights systematically.**