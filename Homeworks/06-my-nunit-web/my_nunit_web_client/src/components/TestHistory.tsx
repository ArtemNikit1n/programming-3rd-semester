import React, { useState, useEffect } from 'react';
import {
    Box,
    Paper,
    Typography,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Chip,
    Button,
    Alert,
    CircularProgress,
    Accordion,
    AccordionSummary,
    AccordionDetails,
    Card,
    CardContent,
    Divider
} from '@mui/material';
import {
    ExpandMore,
    Refresh,
    Delete,
    CheckCircle,
    Error,
    Warning
} from '@mui/icons-material';
import axios from 'axios';
import { TestRun, AssemblyTestResult, TestResult } from '../types';

interface TestHistoryProps {
    currentRun: TestRun | null;
    sx?: any;
}

const TestHistory: React.FC<TestHistoryProps> = ({ currentRun, sx }) => {
    const [history, setHistory] = useState<TestRun[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (currentRun) {
            setHistory(prev => {
                const filtered = prev.filter(r => r.id !== currentRun.id);
                return [currentRun, ...filtered];
            });
        }
    }, [currentRun]);

    const loadHistory = async () => {
        setLoading(true);
        setError(null);
        try {
            const response = await axios.get<TestRun[]>('http://localhost:5221/api/tests/history');
            setHistory(response.data);
        } catch (err: any) {
            if (err.response?.status !== 404 && err.response?.status !== 400) {
                setError(err.response?.data?.error || err.message || 'Failed to load history');
            }
        } finally {
            setLoading(false);
        }
    };

    const clearHistory = async () => {
        try {
            await axios.delete('http://localhost:5221/api/tests/history');
            setHistory([]);
        } catch (err: any) {
            // Не критичная ошибка
            console.error('Clear history error:', err);
        }
    };

    const getStatusIcon = (status: string | number) => {
        const statusStr = status.toString();
        switch (statusStr) {
            case '0':
            case 'Passed': return <CheckCircle color="success" />;
            case '1':
            case 'Failed': return <Error color="error" />;
            case '2':
            case 'Ignored': return <Warning color="warning" />;
            default: return null;
        }
    };

    const getStatusText = (status: string | number) => {
        const statusStr = status.toString();
        switch (statusStr) {
            case '0': return 'Passed';
            case '1': return 'Failed';
            case '2': return 'Ignored';
            default: return statusStr;
        }
    };

    const formatTime = (timeString: string) => {
        try {
            return new Date(timeString).toLocaleString();
        } catch {
            return timeString;
        }
    };

    const calculateStats = (run: TestRun) => {
        let totalPassed = 0;
        let totalFailed = 0;
        let totalIgnored = 0;

        run.assemblyResults.forEach(assembly => {
            assembly.testResults.forEach(test => {
                const status = test.status.toString();
                if (status === '0' || status === 'Passed') totalPassed++;
                else if (status === '1' || status === 'Failed') totalFailed++;
                else if (status === '2' || status === 'Ignored') totalIgnored++;
            });
        });

        return { totalPassed, totalFailed, totalIgnored };
    };

    return (
        <Paper sx={{ p: 3, ...sx }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" gutterBottom>
                    Test History
                </Typography>
                <Box>
                    <Button
                        startIcon={<Refresh />}
                        onClick={loadHistory}
                        disabled={loading}
                        size="small"
                        sx={{ mr: 1 }}
                    >
                        Refresh
                    </Button>
                    <Button
                        startIcon={<Delete />}
                        onClick={clearHistory}
                        color="error"
                        size="small"
                        disabled={history.length === 0}
                    >
                        Clear
                    </Button>
                </Box>
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
                    {error}
                </Alert>
            )}

            {loading ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                    <CircularProgress />
                </Box>
            ) : history.length === 0 ? (
                <Alert severity="info">
                    No test runs recorded yet. Upload assemblies and run tests to see history.
                </Alert>
            ) : (
                <Box>
                    {history.map((run) => {
                        const stats = calculateStats(run);
                        return (
                            <Card key={run.id || Math.random()} sx={{ mb: 2 }}>
                                <CardContent>
                                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                        <Box>
                                            <Typography variant="subtitle1" fontWeight="bold">
                                                Test Run - {formatTime(run.runTime)}
                                            </Typography>
                                            <Typography variant="body2" color="text.secondary">
                                                {run.assemblyResults?.length || 0} assembly(ies)
                                            </Typography>
                                        </Box>
                                        <Box sx={{ display: 'flex', gap: 1 }}>
                                            <Chip
                                                icon={<CheckCircle />}
                                                label={`${stats.totalPassed} Passed`}
                                                color="success"
                                                variant="outlined"
                                                size="small"
                                            />
                                            <Chip
                                                icon={<Error />}
                                                label={`${stats.totalFailed} Failed`}
                                                color="error"
                                                variant="outlined"
                                                size="small"
                                            />
                                            <Chip
                                                icon={<Warning />}
                                                label={`${stats.totalIgnored} Ignored`}
                                                color="warning"
                                                variant="outlined"
                                                size="small"
                                            />
                                        </Box>
                                    </Box>

                                    <Divider sx={{ my: 2 }} />

                                    {(run.assemblyResults || []).map((assembly: AssemblyTestResult, assemblyIndex: number) => {
                                        const assemblyStats = {
                                            total: assembly.testResults?.length || 0,
                                            passed: assembly.testResults?.filter(t => t.status.toString() === '0' || t.status === 'Passed').length || 0,
                                            failed: assembly.testResults?.filter(t => t.status.toString() === '1' || t.status === 'Failed').length || 0,
                                            ignored: assembly.testResults?.filter(t => t.status.toString() === '2' || t.status === 'Ignored').length || 0,
                                        };

                                        return (
                                            <Accordion key={assemblyIndex} sx={{ mb: 1 }}>
                                                <AccordionSummary expandIcon={<ExpandMore />}>
                                                    <Box sx={{ width: '100%', display: 'flex', justifyContent: 'space-between' }}>
                                                        <Typography>{assembly.assemblyName}</Typography>
                                                        <Box sx={{ display: 'flex', gap: 1 }}>
                                                            <Chip label={`Total: ${assemblyStats.total}`} size="small" />
                                                            <Chip
                                                                label={`Passed: ${assemblyStats.passed}`}
                                                                color="success"
                                                                size="small"
                                                                variant="outlined"
                                                            />
                                                            <Chip
                                                                label={`Failed: ${assemblyStats.failed}`}
                                                                color="error"
                                                                size="small"
                                                                variant="outlined"
                                                            />
                                                            <Chip
                                                                label={`Ignored: ${assemblyStats.ignored}`}
                                                                color="warning"
                                                                size="small"
                                                                variant="outlined"
                                                            />
                                                        </Box>
                                                    </Box>
                                                </AccordionSummary>
                                                <AccordionDetails>
                                                    <TableContainer>
                                                        <Table size="small">
                                                            <TableHead>
                                                                <TableRow>
                                                                    <TableCell>Class</TableCell>
                                                                    <TableCell>Method</TableCell>
                                                                    <TableCell>Status</TableCell>
                                                                    <TableCell align="right">Duration (ms)</TableCell>
                                                                    <TableCell>Messages</TableCell>
                                                                </TableRow>
                                                            </TableHead>
                                                            <TableBody>
                                                                {(assembly.testResults || []).map((test: TestResult, testIndex: number) => (
                                                                    <TableRow
                                                                        key={testIndex}
                                                                        sx={{
                                                                            '&:hover': {
                                                                                backgroundColor: 'action.hover',
                                                                                cursor: 'pointer'
                                                                            }
                                                                        }}
                                                                    >
                                                                        <TableCell>{test.testClass}</TableCell>
                                                                        <TableCell>{test.testMethod}</TableCell>
                                                                        <TableCell>
                                                                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                                                {getStatusIcon(test.status)}
                                                                                <Typography variant="body2">{getStatusText(test.status)}</Typography>
                                                                            </Box>
                                                                        </TableCell>
                                                                        <TableCell align="right">
                                                                            {test.durationMs?.toFixed(2) || '0.00'}
                                                                        </TableCell>
                                                                        <TableCell>
                                                                            {(test.messages || []).length > 0 ? test.messages.join(', ') : '-'}
                                                                        </TableCell>
                                                                    </TableRow>
                                                                ))}
                                                            </TableBody>
                                                        </Table>
                                                    </TableContainer>
                                                </AccordionDetails>
                                            </Accordion>
                                        );
                                    })}
                                </CardContent>
                            </Card>
                        );
                    })}
                </Box>
            )}
        </Paper>
    );
};

export default TestHistory;