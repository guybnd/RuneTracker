---
id: SCRATCH-1
title: Scratch 1
status: Todo
priority: None
effort: None
assignee: unassigned
tags: []
createdBy: Guy
updatedBy: User
kind: scratch
history:
  - type: activity
    user: Guy
    date: '2026-09-12T14:14:19.328Z'
    comment: Created ticket.
    id: a-2026-09-12t14-14-19-328z
  - type: agent_session
    sessionId: bc1b49fd-f934-462a-810d-056248483df0
    startedAt: '2026-09-12T14:14:52.799Z'
    status: waiting-input
    progress:
      - timestamp: '2026-09-12T14:15:08.714Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:15:23.722Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:15:38.735Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:15:53.745Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:17:31.382Z'
        message: >-
          Go ahead — what are you looking to add?


          I'll hold off on sketching anything until I hear it. For context when
          you describe it, the pieces available to build on are:


          | Layer | Files |

          |---|---|

          | Capture | `src/OCR/Capture/OcrCaptureStrategy.cs` (PrintWindow / WGC
          / DXGI) |

          | Region resolution | `OcrCaptureRegionResolver.cs`,
          `Poe2WindowResolutionService.cs`, `OcrResolutionProfiles.cs` |

          | Panel detection | `LeaguePanelDetector.cs` |

          | Preprocessing | `OcrImagePreprocessor.cs` (682 lines),
          `OcrPipeline.cs` (row segmentation) |

          | Recognition | `WindowsOcrEngine.cs`, `NativeTesseractEngine.cs` |

          | Post-processing | `OcrTextPostProcessor.cs`,
          `Pricing/ItemNameParser.cs`, `ItemNameTranslator.cs` |

          | Orchestration | `OcrLeagueWindowReader.cs` (1238 lines — the god
          object) |


          There's also a test harness already in place at
          `tests/OcrPricingSimulator/`, which is usually the cheapest place to
          iterate on OCR changes without running the game.
    user: Claude Code
    date: '2026-09-12T14:14:52.799Z'
    enginePid: 9336
    outcome: Claude Code paused — waiting for user input.
    originalProgressCount: 5
    finalMessage: >-
      Go ahead — what are you looking to add?


      I'll hold off on sketching anything until I hear it. For context when you
      describe it, the pieces available to build on are:


      | Layer | Files |

      |---|---|

      | Capture | `src/OCR/Capture/OcrCaptureStrategy.cs` (PrintWindow / WGC /
      DXGI) |

      | Region resolution | `OcrCaptureRegionResolver.cs`,
      `Poe2WindowResolutionService.cs`, `OcrResolutionProfiles.cs` |

      | Panel detection | `LeaguePanelDetector.cs` |

      | Preprocessing | `OcrImagePreprocessor.cs` (682 lines), `OcrPipeline.cs`
      (row segmentation) |

      | Recognition | `WindowsOcrEngine.cs`, `NativeTesseractEngine.cs` |

      | Post-processing | `OcrTextPostProcessor.cs`,
      `Pricing/ItemNameParser.cs`, `ItemNameTranslator.cs` |

      | Orchestration | `OcrLeagueWindowReader.cs` (1238 lines — the god object)
      |


      There's also a test harness already in place at
      `tests/OcrPricingSimulator/`, which is usually the cheapest place to
      iterate on OCR changes without running the game.
  - type: comment
    user: User
    date: '2026-09-12T14:21:50.059Z'
    comment: "i will explain. the tool is for the game path of exile 2. in it there is a 'expedition' game mode and this mode contains various remnants scattered about. \n\nthis tool what it already achieves, is to let you pick the best reward for remnants, but there is a second mechanic to the remnants. they each add their selected mods (only the specially highlighted ones) to the next remnant after it in order. which makes the next one's monsters have the selected mod added to it\n\nto optimize the gameplay,. a player will want to have the most amount of runes added from the remnant, by the end of his run. since they can repeat, and theres less and more optimal ones, i want to add in this tool a way to \n1. show which remnants were already selected so we dont try to double dip\n2. show which ones are more valuable\n3. show on screen, in the selector menu, which ones we may have already picked so are now redundant, which ones have more value, estuff like that.\nit looks like such :\n\npic 1\n\npic 2 when the screen is opened, it shows you the selection of runes. then we can try to optimzie there to pick the correct ones. or show if they are not so valuable\nnote the runes with gilded border are the ones that can carry on. the runes themselves have a color where blue one is x value and puple is y value and gold is most value. between each one theres also more preferable ones ofc\n\n\U0001F4CE image.png, image-2.png"
    id: c-2026-09-12t14-21-50-059z
baselineCommit: 79e13186bd635da01a8d14958a13c8c2d8260bd0
tokenMetadata:
  inputTokens: 483148
  outputTokens: 4661
  costUSD: 0.966771
  costIsEstimated: false
  cacheReadTokens: 419308
  cacheCreationTokens: 63822
needsAction: null
---

