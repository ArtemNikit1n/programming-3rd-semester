export interface TestResult {
    testClass: string;
    testMethod: string;
    status: 'Passed' | 'Failed' | 'Ignored';
    durationMs: number;
    messages: string[];
    exceptionType?: string;
    exceptionMessage?: string;
    stackTrace?: string;
}

export interface AssemblyTestResult {
    assemblyName: string;
    assemblyPath: string;
    testResults: TestResult[];
    totalTests: number;
    passed: number;
    failed: number;
    ignored: number;
}

export interface TestRun {
    id: string;
    runTime: string;
    assemblyResults: AssemblyTestResult[];
    totalPassed: number;
    totalFailed: number;
    totalIgnored: number;
}

export interface UploadResponse {
    filePath: string;
}