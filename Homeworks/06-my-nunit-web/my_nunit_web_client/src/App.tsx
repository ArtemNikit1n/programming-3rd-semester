import React, { useState } from 'react';
import {
  Container,
  Typography,
  Box,
  AppBar,
  Toolbar,
  CssBaseline,
  ThemeProvider,
  createTheme
} from '@mui/material';
import FileUpload from './components/FileUpload';
import TestRunner from './components/TestRunner';
import TestHistory from './components/TestHistory';
import { TestRun } from './types';

const theme = createTheme({
  palette: {
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
  },
});

function App() {
  const [currentRun, setCurrentRun] = useState<TestRun | null>(null);
  const [uploadedFiles, setUploadedFiles] = useState<string[]>([]);
  const [history, setHistory] = useState<TestRun[]>([]);

  const handleTestComplete = (testRun: TestRun) => {
    setCurrentRun(testRun);
  };

  return (
      <ThemeProvider theme={theme}>
        <React.Fragment>
          <CssBaseline />
          <AppBar position="static">
            <Toolbar>
              <Typography variant="h6">MyNUnit Web</Typography>
            </Toolbar>
          </AppBar>
          <Container maxWidth="lg" sx={{ mt: 4 }}>
            <Box sx={{ mb: 4 }}>
              <Typography variant="h4" gutterBottom>
                Unit Test Runner
              </Typography>
              <Typography variant="body1" color="text.secondary">
                Upload DLL assemblies and run unit tests
              </Typography>
            </Box>

            <FileUpload
                onFilesUploaded={setUploadedFiles}
                sx={{ mb: 4 }}
            />

            <TestRunner
                assemblyPaths={uploadedFiles}
                onTestComplete={handleTestComplete}
                sx={{ mb: 4 }}
            />

            <TestHistory
                currentRun={currentRun}
                sx={{ mb: 4 }}
            />
          </Container>
        </React.Fragment>
      </ThemeProvider>
  );
}

export default App;