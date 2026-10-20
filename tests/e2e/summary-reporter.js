import { appendFileSync } from 'node:fs';

/** Writes a Markdown summary of the run to $GITHUB_STEP_SUMMARY (GitHub Actions). */
export default class SummaryReporter {
    constructor() {
        this.results = [];
    }

    onTestEnd(test, result) {
        this.results.push({ title: test.titlePath().slice(2).join(' › '), status: result.status, error: result.error?.message });
    }

    onEnd() {
        const file = process.env.GITHUB_STEP_SUMMARY;
        if (!file) return;
        const failed = this.results.filter((r) => r.status !== 'passed' && r.status !== 'skipped');
        const lines = [
            '### Browser accessibility & keyboard tests',
            '',
            `${this.results.length - failed.length} passed, ${failed.length} failed`,
            '',
        ];
        for (const f of failed) {
            lines.push(`<details><summary>❌ ${f.title}</summary>`, '', '```', (f.error ?? '').slice(0, 4000), '```', '</details>', '');
        }
        appendFileSync(file, lines.join('\n') + '\n');
    }
}
