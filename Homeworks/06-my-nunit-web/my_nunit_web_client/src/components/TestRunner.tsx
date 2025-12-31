import React, { useState } from 'react';
import {
    Box,
    Button,
    Paper,
    Typography,
    Alert,
    LinearProgress,
    Chip,
    CircularProgress
} from '@mui/material';
import { PlayArrow } from '@mui/icons-material';
import axios from 'axios';
import { TestRun } from '../types';

interface TestRunnerProps {
    assemblyPaths: string[];
    onTestComplete: (testRun: TestRun) => void;
    sx?: any;
}

const TestRunner: React.FC<TestRunnerProps> = ({ assemblyPaths, onTestComplete, sx }) => {
    const [running, setRunning] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleRunTests = async () => {
        if (assemblyPaths.length === 0) {
            setError('Please upload at least one assembly first');
            return;
        }

        setRunning(true);
        setError(null);

        try {
            const response = await axios.post<TestRun>('http://localhost:5221/api/tests/run', assemblyPaths);
            onTestComplete(response.data);
        } catch (err: any) {
            setError(err.response?.data?.error || err.message);
        } finally {
            setRunning(false);
        }
    };

    return (
        <Paper sx={{ p: 3, ...sx }}>
            <Typography variant="h6" gutterBottom>
                Run Tests
            </Typography>

            <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                    Assemblies ready for testing: {assemblyPaths.length}
                </Typography>
                {assemblyPaths.map((path, index) => {
                    const fileName = path.split('\\').pop()?.split('/').pop();
                    return (
                        <Chip
                            key={index}
                            label={fileName}
                            size="small"
                            sx={{ mr: 1, mb: 1 }}
                        />
                    );
                })}
            </Box>

            <Button
                variant="contained"
                color="primary"
                startIcon={running ? <CircularProgress size={20} color="inherit" /> : <PlayArrow />}
                onClick={handleRunTests}
                disabled={running || assemblyPaths.length === 0}
                fullWidth
                sx={{ mb: 2 }}
            >
                {running ? 'Running Tests...' : 'Start Testing'}
            </Button>

            {error && (
                <Alert severity="error" sx={{ mt: 2 }}>
                    {error}
                </Alert>
            )}

            {running && (
                <LinearProgress sx={{ mt: 2 }} />
            )}
        </Paper>
    );
};

export default TestRunner;